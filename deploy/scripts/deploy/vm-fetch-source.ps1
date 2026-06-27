# deploy/scripts/deploy/vm-fetch-source.ps1
# Fetches the kash-cards source onto the VM for build-on-box, via an INCREMENTAL
# git clone/fetch (the model the sibling runegate-infra uses). The box keeps a
# persistent clone at TargetDir; each run transfers only the delta:
#
#   * first run (no clone, or a non-git tree left by the old zipball path):
#     `git clone --branch <branch>` a fresh copy.
#   * every run after: `git fetch` + `git reset --hard origin/<branch>` +
#     `git clean -ffd` -- pulls only changed objects (seconds), forces the tree to
#     match origin's tip, and drops stray untracked files. `-ffd` (NOT `-ffdx`)
#     deliberately KEEPS gitignored content: the `packages/` NuGet cache and the
#     on-box `deploy/config` survive, so a redeploy needn't re-restore or re-push
#     them. The obj/ and bin/ that survive are cleaned per-project by deploy-iis.
#
# This replaces the previous full-zipball download, which re-fetched and
# re-extracted the ENTIRE tree (slow Expand-Archive) on every deploy.
#
# Auth (private repo): the token is pulled from Key Vault via the VM managed
# identity (secret REPO-TOKEN) and handed to git as a per-command
# `http.extraheader` -- it is NEVER written into .git/config or the remote URL
# (the remote stays a plain https URL) and is never logged. git itself must
# already be on the box; this script resolves it and fails loudly if absent.
param(
    [Parameter(Mandatory = $true)][string]$RepoUrl,   # https://github.com/<owner>/<repo>[.git] or git@...
    [string]$Branch = 'main',
    [string]$TargetDir = 'C:\src\kash-cards',
    [string]$KvName,                                   # optional: pull REPO-TOKEN for a private repo
    [string]$Token                                     # optional PAT fallback
)
$ErrorActionPreference = 'Stop'
function Step($m) { Write-Host "[..] $m" }
function Ok($m)   { Write-Host "[ok] $m" }
function Warn($m) { Write-Host "[!!] $m" -ForegroundColor Yellow }
function Die($m)  { Write-Host "[xx] $m"; exit 1 }

# -- Resolve git (already installed on the box; we do not install it here). ----
function Resolve-Git {
    $c = Get-Command git -ErrorAction SilentlyContinue
    if ($c) { return $c.Source }
    foreach ($p in @(
        'C:\Program Files\Git\cmd\git.exe',
        'C:\Program Files\Git\bin\git.exe',
        'C:\Program Files (x86)\Git\cmd\git.exe',
        'C:\Tools\git\cmd\git.exe',
        'C:\Tools\git\bin\git.exe'
    )) { if (Test-Path $p) { return $p } }
    return $null
}
$git = Resolve-Git
if (-not $git) { Die "git not found on PATH or the well-known install locations -- install git on the VM (the deploy now fetches incrementally, not via zipball)" }
Ok "git: $git"

# -- Derive owner/repo and a plain https clone URL (token never goes in the URL).
$u = $RepoUrl -replace '\.git$', ''
if ($u -notmatch 'github\.com[:/]+([^/]+)/([^/]+)/?$') { Die "RepoUrl is not a recognized GitHub URL: $RepoUrl" }
$owner = $Matches[1]; $repo = $Matches[2]
$cloneUrl = "https://github.com/$owner/$repo.git"

# -- Token (private repo): prefer Key Vault via the VM managed identity. --------
# Resolve az by full path, NOT via PATH alone: this script runs under
# `az vm run-command` (guest-agent/SYSTEM context) whose PATH is captured when the
# agent starts and does NOT pick up an az install that landed afterward -- so a bare
# `Get-Command az` finds nothing, the token read silently no-ops, and the private
# clone runs unauthenticated and fails. Same stale-PATH reason this script resolves
# git by full path (Resolve-Git) and inject-secrets.ps1 resolves az the same way.
$az = (Get-Command az -ErrorAction SilentlyContinue).Source
if (-not $az) {
    foreach ($cand in @(
        'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd',
        'C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
    )) { if (Test-Path $cand) { $az = $cand; break } }
}
if (-not $Token -and $KvName -and $az) {
    & $az login --identity --output none 2>$null
    if ($LASTEXITCODE -eq 0) {
        $kvTok = (& $az keyvault secret show --vault-name $KvName --name 'REPO-TOKEN' --query value -o tsv 2>$null)
        if (-not [string]::IsNullOrWhiteSpace($kvTok)) { $Token = $kvTok; Step 'using git token from Key Vault (REPO-TOKEN)' }
    }
}

# Auth via git's env-var config injection (GIT_CONFIG_COUNT, git >= 2.31), NOT a
# `-c http.extraheader=...` arg: a `-c` value lands in the process command line
# (readable via WMI/Get-CimInstance while git runs), whereas the env block is not.
# The token is therefore never on disk (no .git/config, no token in the remote
# URL) and never in argv. GitHub HTTPS accepts a PAT / installation token as basic
# auth with the username 'x-access-token'. Cleared in the finally below.
function Set-GitAuthEnv {
    param([string]$Tok)
    if (-not $Tok) { return }
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("x-access-token:$Tok"))
    $env:GIT_CONFIG_COUNT   = '1'
    $env:GIT_CONFIG_KEY_0   = 'http.extraheader'
    $env:GIT_CONFIG_VALUE_0 = "AUTHORIZATION: basic $b64"
}
function Clear-GitAuthEnv {
    Remove-Item Env:GIT_CONFIG_COUNT, Env:GIT_CONFIG_KEY_0, Env:GIT_CONFIG_VALUE_0 -ErrorAction SilentlyContinue
}

# -- git helper: run git and gate strictly on $LASTEXITCODE. Custom Die messages
# never echo the args, and auth rides in the environment (not argv), so the token
# can't leak through an error path.
#
# The parameter is $GitArgs, NOT $Args: `$Args` is a PowerShell AUTOMATIC variable,
# and declaring a param with that name does NOT bind the passed array to it -- it
# stays empty, so `& $git @Args` runs git with NO arguments. git then prints its
# usage and exits non-zero, and the gate Dies with a misleading "git clone failed
# (token valid?)". This is the real reason the build-on-box fetch never worked:
# every git op ran argument-less. (Verified on-box: a param named $Args binds
# Count=0; renamed to $GitArgs it binds Count=1 and git runs.)
#
# Also run git under 'Continue', not the script-level 'Stop': git streams progress
# to stderr, and a 2>&1 under 'Stop' would turn the first stderr record into a
# terminating error mid-run. Capture the merged stream and read the real exit code
# (the runegate-infra pattern).
function Git-Do {
    param([string[]]$GitArgs, [string]$ErrMsg)
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $null = & $git @GitArgs 2>&1 } finally { $ErrorActionPreference = $prev }
    if ($LASTEXITCODE -ne 0) { Die $ErrMsg }
}

$gitDir = Join-Path $TargetDir '.git'
$isRepo = (Test-Path $TargetDir) -and (Test-Path $gitDir)

try {
    Set-GitAuthEnv $Token
    if ($isRepo) {
        Step "existing clone at $TargetDir -- fetch $Branch + reset --hard"
        Git-Do @('-C', $TargetDir, 'fetch', '--quiet', 'origin', $Branch) `
            "git fetch origin $Branch failed (token valid? branch exists on origin?)"

        # Warn (don't fail) if the SYSTEM-owned tree has local tracked edits the
        # reset is about to discard -- an operator may have hand-patched on the box.
        try { & $git -C $TargetDir diff --quiet HEAD 2>&1 | Out-Null } catch { }
        if ($LASTEXITCODE -ne 0) {
            Warn "working tree has local changes; 'reset --hard' will discard them. Copy them off the VM first if they matter."
            $global:LASTEXITCODE = 0
        }

        Git-Do @('-C', $TargetDir, 'reset', '--hard', '--quiet', "origin/$Branch") `
            "git reset --hard origin/$Branch failed"

        # Keep HEAD on a sanely named local branch (handles a REPO_BRANCH switch
        # and avoids detached HEAD); the reset above already matched the tree.
        $cur = (& $git -C $TargetDir rev-parse --abbrev-ref HEAD 2>$null)
        if ($cur -ne $Branch) {
            Git-Do @('-C', $TargetDir, 'checkout', '--quiet', '-B', $Branch, "origin/$Branch") `
                "git checkout -B $Branch origin/$Branch failed"
        }

        # Drop stray untracked files but KEEP gitignored caches/config (no -x).
        Git-Do @('-C', $TargetDir, 'clean', '-ffd', '--quiet') "git clean failed"
    } else {
        if (Test-Path $TargetDir) {
            Warn "$TargetDir exists but is not a git repo (legacy zipball tree) -- removing and cloning fresh"
            Remove-Item -Recurse -Force $TargetDir
        }
        $parent = Split-Path $TargetDir -Parent
        if ($parent -and -not (Test-Path $parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
        Step "cloning $owner/$repo@$Branch -> $TargetDir"
        Git-Do @('clone', '--quiet', '--branch', $Branch, $cloneUrl, $TargetDir) `
            "git clone failed for $owner/$repo on branch $Branch (token valid? branch exists?)"
    }
} finally {
    Clear-GitAuthEnv
}

$sha = (& $git -C $TargetDir rev-parse --short HEAD 2>$null)
Ok "source at $TargetDir (from $owner/$repo@$Branch @ $sha)"

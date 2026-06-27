#!/usr/bin/env bash
# deploy/scripts/perimeter/rdp-access.sh -- DIY just-in-time access to the NSG-dark money box.
#
# The box has NO inbound by default (NSG-dark; managed via `az vm run-command`, control plane).
# To RDP in and inspect/debug, we briefly add an NSG allow rule scoped to the OPERATOR'S CURRENT
# public IP, then re-seal. This mirrors the `Allow-RDP` rule the provisioner already manages
# (azure-vm-provision.sh), just toggled on demand -- so the box stays dark by default and is
# exposed (RDP only) to one IP for the duration of the work.
#
# NOTE: the automated deploy does NOT use this -- it runs the on-box `update` via a single
# `az vm run-command` (control plane, no inbound). This script is for OPERATOR access only; we
# deliberately do NOT open WinRM/SSH (a new inbound listener on a money box is attack surface).
#
#   ENV=dev rdp-access.sh open  [IP]   allow RDP(3389) from IP (default: auto-detect public IP)
#   ENV=dev rdp-access.sh close        re-seal: deny RDP (box dark again)
#   ENV=dev rdp-access.sh status       show the current rule state
#
# `open` is idempotent; `close` is always safe (re-seals even if already closed). The NSG change
# rides the Azure management plane -- it does NOT require any inbound to the box to take effect.
set -euo pipefail

ENV="${ENV:-dev}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"   # -> deploy/
CFG="$SCRIPT_DIR/config/.env.provision.${ENV}"
[[ -f "$CFG" ]] || { printf '[xx] missing %s\n' "$CFG" >&2; exit 1; }
# shellcheck disable=SC1090
. "$CFG"
: "${COMPUTE_RG:?set in $CFG}" : "${NSG_NAME:?set in $CFG}"

log() { printf '\n=== %s ===\n' "$*"; }
ok()  { printf '[ok] %s\n' "$*"; }
die() { printf '[xx] %s\n' "$*" >&2; exit 1; }

# Auto-detect the operator's public IP via a public echo service. This is an ordinary outbound
# web call from THIS machine -- nothing to do with the box -- so there is no chicken-and-egg.
detect_ip() {
  local ip
  ip=$(curl -fsS --max-time 10 https://api.ipify.org 2>/dev/null) \
    || ip=$(curl -fsS --max-time 10 https://ifconfig.me 2>/dev/null) \
    || die "could not auto-detect public IP -- pass it explicitly: open <IP>"
  printf '%s' "$ip" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' \
    || die "auto-detected value is not an IPv4: '$ip' (split-tunnel VPN? pass the IP explicitly)"
  printf '%s' "$ip"
}

# Create-or-update an NSG rule (same shape as azure-vm-provision.sh::ensure_nsg_rule).
set_rule() { # name priority port source access desc
  local name="$1" priority="$2" port="$3" source="$4" access="$5" desc="$6"
  if az network nsg rule show -g "$COMPUTE_RG" --nsg-name "$NSG_NAME" -n "$name" >/dev/null 2>&1; then
    az network nsg rule update -g "$COMPUTE_RG" --nsg-name "$NSG_NAME" -n "$name" \
      --priority "$priority" --access "$access" --protocol Tcp --direction Inbound \
      --source-address-prefixes "$source" --destination-address-prefixes '*' \
      --destination-port-ranges "$port" --description "$desc" >/dev/null
  else
    az network nsg rule create -g "$COMPUTE_RG" --nsg-name "$NSG_NAME" -n "$name" \
      --priority "$priority" --access "$access" --protocol Tcp --direction Inbound \
      --source-address-prefixes "$source" --destination-address-prefixes '*' \
      --destination-port-ranges "$port" --description "$desc" >/dev/null
  fi
  ok "NSG $name: access=$access source=$source port=$port"
}

ACTION="${1:-}"; IP_ARG="${2:-}"
case "$ACTION" in
  open)
    IP="${IP_ARG:-$(detect_ip)}"
    [[ "$IP" == */* ]] || IP="$IP/32"
    # Validate BEFORE opening. Only the auto-detect path is regex-checked; an EXPLICIT arg
    # ('0.0.0.0/0', '*', 'Internet', 'Any', an Azure service tag, a fat-fingered CIDR) would
    # otherwise pass straight to --source-address-prefixes and expose 3389 to the whole internet
    # (priority 110 overrides the default deny; RDP creds become the only barrier). Require a real
    # IPv4 address or CIDR no broader than /24.
    if [[ ! "$IP" =~ ^(25[0-5]|2[0-4][0-9]|1?[0-9]?[0-9])(\.(25[0-5]|2[0-4][0-9]|1?[0-9]?[0-9])){3}/([0-9]|[12][0-9]|3[0-2])$ ]]; then
      die "refusing to open RDP to '$IP' -- not a valid single IPv4 address or CIDR"
    fi
    if (( ${IP##*/} < 24 )); then
      die "refusing to open RDP to '$IP' -- /${IP##*/} is too broad (need /24 or narrower; a single host is /32)"
    fi
    [[ "${IP%%/*}" == "0.0.0.0" ]] && die "refusing to open RDP to 0.0.0.0 -- that is the whole internet"
    log "OPEN perimeter to $IP (RDP 3389)"
    set_rule "Allow-RDP" 110 3389 "$IP" Allow "DIY-JIT: RDP open to operator IP; 'close' re-seals to Deny"
    ok "perimeter OPEN to $IP -- run 'close' when done so the box re-seals"
    ;;
  close)
    log "CLOSE perimeter (re-seal RDP) + verify"
    # Re-sealing is safety-critical: RETRY on a transient az failure, then VERIFY by reading the
    # rule back -- never report 'closed' on an unconfirmed write. Deny + a TEST-NET (RFC5737)
    # placeholder source so no live operator IP lingers in the rule.
    sealed=false
    for attempt in 1 2 3; do
      set_rule "Allow-RDP" 110 3389 "192.0.2.0/24" Deny "DIY-JIT: RDP sealed (dark)" 2>/dev/null || true
      acc=$(az network nsg rule show -g "$COMPUTE_RG" --nsg-name "$NSG_NAME" -n "Allow-RDP" --query access -o tsv 2>/dev/null || true)
      if [[ "$acc" == "Deny" ]]; then sealed=true; break; fi
      [[ $attempt -lt 3 ]] && { printf '[..] re-seal attempt %s did not confirm Deny; retrying in 5s...\n' "$attempt"; sleep 5; }
    done
    if $sealed; then
      ok "perimeter CLOSED -- box is dark again (verified Allow-RDP=Deny)"
    else
      die "PERIMETER MAY STILL BE OPEN -- could not confirm Allow-RDP=Deny after 3 tries. RE-RUN 'rdp-close' NOW, or set the NSG rule to Deny manually."
    fi
    ;;
  status)
    az network nsg rule show -g "$COMPUTE_RG" --nsg-name "$NSG_NAME" -n "Allow-RDP" \
      --query "{name:name, access:access, source:sourceAddressPrefix, sourcePrefixes:sourceAddressPrefixes, port:destinationPortRange}" \
      -o jsonc 2>/dev/null || printf 'Allow-RDP: (rule absent -- dark)\n'
    ;;
  *) die "usage: ENV=$ENV $(basename "$0") open [IP] | close | status" ;;
esac

# Crypto-at-rest cutover runbook

How to roll out the move from the old reversible password/secret cipher to one-way
**bcrypt** (passwords, API secrets) + authenticated **AES-256-GCM** (2FA seeds), and how
to scrub the old recoverable values from a database that already holds them.

## What changed in the code

- Passwords and API `SecretKey`s are now one-way **bcrypt** hashes
  (`QryptoCard.INT.Security.PasswordHasher`, work factor 12). Login/validation verify the
  plaintext against the stored hash; the dashboard/API clients send the plaintext over the
  internal channel (the old `EncryptAPP` wire-encryption is removed).
- TOTP/2FA seeds use **AES-256-GCM** with a random IV per value
  (`QryptoCard.INT.Security.AesUtility`), under a new master key **`KASH_DATA_KEY`**
  (32-byte hex).
- The old `DBKEY` / `APPKEY` master keys and the Rijndael cipher (`QryptoCard.Sec.Secure`,
  reduced to Base64 helpers) are **retired** — no code reads them.

## Consequence: deploying the code is itself a forced reset

bcrypt verification cannot match the old Rijndael ciphertext, so **the moment the new code
is live, every existing password stops working** and users must reset. Plan the rollout
around that:

1. **A working password-reset link is a hard prerequisite.** The reset email must point at
   the environment that's actually serving (the reset-link base URL must resolve to the live
   host). Verify a real reset round-trips on the target host *before* cutover, or users will
   have no way back in.
2. Sequence this with the domain cutover if the live host is changing, so reset links land
   on the right host.

## Steps

### 1. Provision `KASH_DATA_KEY`
Generate a 32-byte key and seed it into the environment's vault, then Key Vault:
```
openssl rand -hex 32        # 64 hex chars
```
Put it in `secrets/.vault.<env>` as `KASH_DATA_KEY=<hex>`, then
`ENV=<env> ./deploy/scripts/secrets/seed-kv-secrets.sh`. It is a **hard startup dependency**
of the INT tier (preloaded in `Global.asax`) — the pool faults at boot if it's missing or
not 64 hex chars. `DBKEY`/`APPKEY` are no longer needed and can be dropped from the vault.

### 2. Deploy the code + inject secrets
Deploy the new build to all tiers and re-inject pool secrets so `KASH_DATA_KEY` is written
into the INT pool environment (and the retired `DBKEY`/`APPKEY` drop out):
```
ENV=<env> ./deploy/deploy.sh update
```
A fresh provision needs nothing further for credentials — `vm-seed.ps1` now bcrypt-hashes the
bootstrap admin (and dev smoke/demo users) via the app's BCrypt library, so a newly-seeded
admin can log in immediately.

### 3. Scrub the old recoverable values (existing databases only)
On a database that already holds rows under the old leaked-key cipher, run the one-off scrub
to remove the recoverable ciphertext. **Destructive and deliberate** — it resets every
password, removes all 2FA enrolments, and neutralises all API secrets:
```
sqlcmd -S <server> -d <db> -E -b -i deploy/sql/oneoff/crypto-at-rest-scrub.sql
```
(Skip on a fresh provision — there are no legacy rows.)

### 4. Restore bootstrap admin access
The scrub also resets the bootstrap admin, and `seed-admin.sql` is insert-only (it will not
update an existing row), so set the admin password explicitly with a fresh bcrypt hash.
Generate the hash with the app's own hasher (work factor 12) — e.g. via the same BCrypt
library `vm-seed.ps1` loads:
```
Add-Type -Path '<path>\packages\BCrypt.Net-Next.4.0.3\lib\net462\BCrypt.Net-Next.dll'
[BCrypt.Net.BCrypt]::HashPassword('<new-admin-password>', 12)
```
Then:
```
UPDATE dbo.tblM_Admin SET Password = N'<bcrypt-hash>' WHERE Email = N'<admin-email>';
```

### 5. Notify users + re-issue API keys
- Tell users their passwords were reset for security and to use "Forgot password".
- Re-issue any API credentials that were in use (the old `SecretKey`s are neutralised and the
  keys deactivated). **Keep re-issued API secrets to ≤ 72 bytes** — bcrypt silently ignores
  input past 72 bytes, so a longer secret would cap effective entropy and let two secrets
  sharing a 72-byte prefix verify as equal. The generated GUID-pair secret (64 bytes) is fine.
- Affected users re-enrol 2FA (expected near-zero).

## Verification
- INT pool starts (no missing-`KASH_DATA_KEY` startup fault).
- A user password reset round-trips on the live host and the new password logs in.
- The bootstrap admin logs in with the password set in step 4.
- A 2FA enrol → the stored seed decrypts on read (AES-256-GCM round-trip).

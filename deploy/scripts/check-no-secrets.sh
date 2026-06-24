#!/usr/bin/env bash
# Secret-leak guard (grep-pin). Fails (exit 1) if secret-shaped material appears in
# tracked source (*.cs, *.config). Wire into CI and/or a pre-commit hook. Uses generic
# structural patterns (so it catches NEW secrets, not just the old leaked values) plus a
# backstop list of known-leaked fragments.
set -u
cd "$(git rev-parse --show-toplevel 2>/dev/null || echo .)" || exit 2

FAIL=0
scan() {
  local desc="$1"; local pat="$2"
  local hits
  hits=$(git ls-files -z '*.cs' '*.config' | xargs -0 grep -nEH "$pat" 2>/dev/null)
  if [ -n "$hits" ]; then
    echo "FAIL [$desc]:"
    echo "$hits" | sed -E 's/(password=)[^;"&<]*/\1<...>/g' | cut -c1-160
    FAIL=1
  fi
}

scan "RSA private key (XML)"            '<D>[A-Za-z0-9+/=]{20,}</D>'
scan "PEM private key"                  'BEGIN [A-Z ]*PRIVATE KEY'
scan "non-empty connection password"   'password=[^;"&< ]'
scan "known leaked fragments"          'k3Yh45H|olkebbjshtdbzcae|d6246d5d-0804|8128cdfe-24e8|19073d27f59d|3bdE4b10|B3wZnoBNJv|9rAFwnUUup|jrwxdabkuhuy'

if [ "$FAIL" -ne 0 ]; then
  echo ""
  echo "Secret-shaped material found in tracked source. Move it to deploy/secrets/.vault"
  echo "and read it via QryptoCard.Sec.SecretsConfig. Commit blocked."
  exit 1
fi
echo "OK: no secret-shaped material in tracked source."

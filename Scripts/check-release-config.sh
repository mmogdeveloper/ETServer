#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
release_config="$repo_root/Config/Json/s/StartConfig/Release"

if rg -n '"(InnerIP|OuterIP)":"127\.0\.0\.1"|"DBConnection":"mongodb://127\.0\.0\.1' "$release_config"; then
  if [[ "${ALLOW_LOOPBACK_RELEASE:-0}" != "1" ]]; then
    echo "Release config still contains loopback IP or unauthenticated local MongoDB. Set production values before release." >&2
    exit 1
  fi
  echo "WARNING: loopback Release config explicitly allowed for local pipeline verification." >&2
fi

if ! rg -q '"DBName":"[^\"]+"' "$release_config/StartZoneConfig.txt"; then
  echo "Release database name is missing." >&2
  exit 1
fi

echo "Release configuration check passed."

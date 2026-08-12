#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${1:-Debug}"

dotnet build "$repo_root/DotNet/Robot/Hotfix/DotNet.Robot.Hotfix.csproj" \
  --configuration "$configuration" \
  --no-restore \
  --no-incremental \
  -p:NuGetAudit=false \
  --disable-build-servers \
  -m:1

dotnet build "$repo_root/DotNet/Robot/App/DotNet.Robot.App.csproj" \
  --configuration "$configuration" \
  --no-restore \
  --no-incremental \
  -p:NuGetAudit=false \
  --disable-build-servers \
  -m:1

test -f "$repo_root/Bin/Robot.App.dll"
test -f "$repo_root/Bin/Robot.Model.dll"
test -f "$repo_root/Bin/Robot.Hotfix.dll"

echo "Robot build complete: configuration=$configuration output=$repo_root/Bin"

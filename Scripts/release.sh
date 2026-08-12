#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
version="$(tr -d '[:space:]' < "$repo_root/VERSION")"

"$repo_root/Scripts/check-release-config.sh"

dotnet restore "$repo_root/ET.sln"
dotnet restore "$repo_root/DotNet/Robot/App/DotNet.Robot.App.csproj"

if [[ "${SKIP_VULNERABILITY_CHECK:-0}" != "1" ]]; then
  "$repo_root/Scripts/check-vulnerabilities.sh"
else
  echo "WARNING: vulnerability check explicitly skipped." >&2
fi

if [[ -n "$(git -C "$repo_root" status --porcelain)" ]] && [[ "${ALLOW_DIRTY_RELEASE:-0}" != "1" ]]; then
  echo "Release requires a clean Git worktree." >&2
  exit 1
fi

if [[ "$version" == *-* ]] && [[ "${ALLOW_PRERELEASE:-0}" != "1" ]]; then
  echo "VERSION is a prerelease ($version). Set a stable version or explicitly allow prerelease." >&2
  exit 1
fi

"$repo_root/Scripts/publish.sh"

commit="$(git -C "$repo_root" rev-parse HEAD)"
build_time="$(date -u '+%Y-%m-%dT%H:%M:%SZ')"
sdk_version="$(dotnet --version)"

for package in Server Robot; do
  package_root="$repo_root/Publish/$package"
  cat > "$package_root/RELEASE.json" <<EOF
{
  "version": "$version",
  "gitCommit": "$commit",
  "buildTimeUtc": "$build_time",
  "dotnetSdk": "$sdk_version",
  "package": "$package",
  "deploymentModel": "framework-dependent",
  "targetFramework": "net8.0"
}
EOF

  (
    cd "$package_root"
    find . -type f ! -name SHA256SUMS -print | LC_ALL=C sort | while IFS= read -r file; do
      shasum -a 256 "$file"
    done > SHA256SUMS
  )
done

echo "Release artifacts created: version=$version commit=$commit"

#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_root="$repo_root/Publish"
staging_root="$(mktemp -d "${TMPDIR:-/tmp}/etserver-publish.XXXXXX")"
trap 'rm -rf "$staging_root"' EXIT

# All projects share this output directory. It must be empty so package upgrades
# cannot leave stale assemblies that are still copied into a release artifact.
mkdir -p "$repo_root/Bin"
find "$repo_root/Bin" -mindepth 1 -maxdepth 1 -type f -delete

dotnet build "$repo_root/DotNet/Hotfix/DotNet.Hotfix.csproj" \
  --configuration Release --no-restore --no-incremental -p:NuGetAudit=false --disable-build-servers -m:1
dotnet build "$repo_root/DotNet/App/DotNet.App.csproj" \
  --configuration Release --no-restore --no-incremental -p:NuGetAudit=false --disable-build-servers -m:1
"$repo_root/Scripts/build-robot.sh" Release

mkdir -p "$staging_root/Server/Bin" "$staging_root/Robot/Bin"
for source in "$repo_root"/Bin/*; do
  [[ -f "$source" ]] || continue
  name="$(basename "$source")"
  case "$name" in
    Robot.*|Tool|Tool.*) ;;
    *) cp "$source" "$staging_root/Server/Bin/" ;;
  esac
  case "$name" in
    App|App.*|Tool|Tool.*) ;;
    *) cp "$source" "$staging_root/Robot/Bin/" ;;
  esac
done

cp -R "$repo_root/Config" "$staging_root/Server/Config"
cp -R "$repo_root/Config" "$staging_root/Robot/Config"
cp "$repo_root/Config/NLog/NLog.Release.config" "$staging_root/Server/Config/NLog/NLog.config"
cp "$repo_root/Config/NLog/NLog.Release.config" "$staging_root/Robot/Config/NLog/NLog.config"

mkdir -p "$publish_root"
rm -rf "$publish_root/Server" "$publish_root/Robot"
mv "$staging_root/Server" "$publish_root/Server"
mv "$staging_root/Robot" "$publish_root/Robot"

test -f "$publish_root/Server/Bin/App.dll"
test -f "$publish_root/Server/Bin/Hotfix.dll"
test ! -f "$publish_root/Server/Bin/Robot.App.dll"
test ! -f "$publish_root/Server/Bin/Robot.Hotfix.dll"
test -f "$publish_root/Robot/Bin/Robot.App.dll"
test -f "$publish_root/Robot/Bin/Robot.Hotfix.dll"
test ! -f "$publish_root/Robot/Bin/App.dll"

echo "Publish complete: $publish_root/Server and $publish_root/Robot"

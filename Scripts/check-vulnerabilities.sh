#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
result_dir="$(mktemp -d "${TMPDIR:-/tmp}/etserver-vulnerabilities.XXXXXX")"
trap 'rm -rf "$result_dir"' EXIT

targets=(
  "$repo_root/ET.sln"
  "$repo_root/DotNet/Robot/App/DotNet.Robot.App.csproj"
)

for index in "${!targets[@]}"; do
  dotnet list "${targets[$index]}" package --vulnerable --include-transitive --format json > "$result_dir/$index.json"
done

python3 - "$result_dir" <<'PY'
import json
import pathlib
import sys

vulnerabilities = []
for report_path in pathlib.Path(sys.argv[1]).glob("*.json"):
    with report_path.open(encoding="utf-8-sig") as stream:
        report = json.load(stream)
    for project in report.get("projects", []):
        for framework in project.get("frameworks", []):
            for key in ("topLevelPackages", "transitivePackages"):
                for package in framework.get(key, []):
                    for vulnerability in package.get("vulnerabilities", []):
                        vulnerabilities.append((project.get("path"), package.get("id"), vulnerability))

if vulnerabilities:
    for project, package, vulnerability in vulnerabilities:
        print(f"vulnerable package: project={project} package={package} severity={vulnerability.get('severity')} advisory={vulnerability.get('advisoryurl')}", file=sys.stderr)
    raise SystemExit(1)

print("NuGet vulnerability check passed: no known vulnerable packages.")
PY

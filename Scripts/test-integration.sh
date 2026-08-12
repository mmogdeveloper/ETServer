#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$repo_root/Scripts/publish.sh"
"$repo_root/Tests/Integration/robot-worker.sh"

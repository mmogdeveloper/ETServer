#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
server_bin="$repo_root/Publish/Server/Bin"
robot_bin="$repo_root/Publish/Robot/Bin"
run_id="RobotIntegration_$(date +%Y%m%d%H%M%S)_$$"
server_pid=""
robot_pid=""

cleanup() {
  if [[ -n "$robot_pid" ]] && kill -0 "$robot_pid" 2>/dev/null; then kill -TERM "$robot_pid"; wait "$robot_pid" || true; fi
  if [[ -n "$server_pid" ]] && kill -0 "$server_pid" 2>/dev/null; then kill -TERM "$server_pid"; wait "$server_pid" || true; fi
}
trap cleanup EXIT

port_in_use() {
  lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1 || lsof -nP -iUDP:"$1" >/dev/null 2>&1
}

for port in 20001 30002 30003 30004 30300 30301 30302 30303 30304; do
  if port_in_use "$port"; then
    echo "required port is already in use: $port" >&2
    exit 1
  fi
done

if ! lsof -nP -iTCP:27017 -sTCP:LISTEN >/dev/null 2>&1; then
  echo "MongoDB is not listening on 127.0.0.1:27017" >&2
  exit 1
fi

bash -c 'cd "$1" && exec dotnet App.dll --AppType=Server --StartConfig=StartConfig/Localhost --Process=1 --Develop=1 --Console=0 --LogLevel=3' _ "$server_bin" &
server_pid=$!

server_ready="$repo_root/Publish/Server/Logs/000001.4294967295.Main.Info.log"
for _ in $(seq 1 60); do
  if lsof -nP -iTCP:30300 -sTCP:LISTEN >/dev/null 2>&1 &&
     lsof -nP -iUDP:30002 >/dev/null 2>&1; then break; fi
  if ! kill -0 "$server_pid" 2>/dev/null; then echo "server exited during startup" >&2; exit 1; fi
  sleep 0.25
done
lsof -nP -iTCP:30300 -sTCP:LISTEN >/dev/null 2>&1
lsof -nP -iUDP:30002 >/dev/null 2>&1

bash -c 'cd "$1" && exec dotnet Robot.App.dll --AppType=RobotWorker --StartConfig=StartConfig/Localhost --RobotCount=10 --RobotInterval=50 --RobotAccountPrefix="$2" --Console=0 --LogLevel=3' _ "$robot_bin" "$run_id" &
robot_pid=$!

summary="$repo_root/Publish/Robot/Logs/000001.4294967295.Main.Info.log"
expected_summary="robot batch complete: prefix=$run_id total=10 ready=10 failed=0"
for _ in $(seq 1 120); do
  if [[ -f "$summary" ]] && grep -F "$expected_summary" "$summary" >/dev/null; then break; fi
  if ! kill -0 "$robot_pid" 2>/dev/null; then echo "robot exited before successful summary" >&2; exit 1; fi
  sleep 0.25
done
grep -F "$expected_summary" "$summary" >/dev/null

kill -TERM "$robot_pid"
wait "$robot_pid"
robot_exit=$?
robot_pid=""
[[ "$robot_exit" -eq 0 ]]

kill -TERM "$server_pid"
wait "$server_pid"
server_exit=$?
server_pid=""
[[ "$server_exit" -eq 0 ]]

for port in 20001 30002 30003 30004 30300 30301 30302 30303 30304; do
  ! port_in_use "$port"
done

echo "Robot integration passed: ready=10 failed=0 robot_exit=$robot_exit server_exit=$server_exit"

#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [[ "${SKIP_RUNTIME_COMPILE:-0}" != 1 ]]; then bash "$here/run-compile.sh"; fi
library="${GOAL_RESET_LIBRARY:-$here/AllRuntime.dll}"
${MCS:-mcs} -r:"$library" -r:System.Runtime.Serialization -out:"$here/GoalResetHarness.exe" "$here/GoalResetHarness.cs"
${MONO:-mono} "$here/GoalResetHarness.exe"

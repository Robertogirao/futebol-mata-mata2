#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [[ "${SKIP_RUNTIME_COMPILE:-0}" != 1 ]]; then bash "$here/run-compile.sh"; fi
${MCS:-mcs} -r:"$here/AllRuntime.dll" -r:System.Runtime.Serialization -out:"$here/AudioSelectionHarness.exe" "$here/AudioSelectionHarness.cs"
${MONO:-mono} "$here/AudioSelectionHarness.exe"

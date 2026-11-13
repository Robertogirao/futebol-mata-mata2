#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [[ "${SKIP_RUNTIME_COMPILE:-0}" != 1 ]]; then bash "$here/../runtime/run-compile.sh"; fi
${MCS:-mcs} -r:"$here/../runtime/AllRuntime.dll" -r:System.Runtime.Serialization -out:"$here/PresentationHarness.exe" "$here/Harness.cs"
cp "$here/../runtime/AllRuntime.dll" "$here/AllRuntime.dll"
${MONO:-mono} "$here/PresentationHarness.exe" | tee "$here/results.txt"

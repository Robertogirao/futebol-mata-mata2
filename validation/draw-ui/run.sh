#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
bash "$here/../runtime/run-compile.sh"
cp "$here/../runtime/AllRuntime.dll" "$here/AllRuntime.dll"
${MCS:-mcs} -r:"$here/../runtime/AllRuntime.dll" -r:System.Runtime.Serialization -out:"$here/Harness.exe" "$here/Harness.cs"
${MONO:-mono} "$here/Harness.exe"

#!/bin/bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
bash "$here/run-compile.sh"
${MCS:-mcs} -r:"$here/AllRuntime.dll" -r:System.Runtime.Serialization -out:"$here/RuntimeHarness.exe" "$here/RuntimeHarness.cs"
${MONO:-mono} "$here/RuntimeHarness.exe"

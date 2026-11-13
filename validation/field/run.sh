#!/bin/bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
source="$here/../../Projeto/Assets/FutebolDiscos/Scripts"
${MCS:-mcs} -r:System.Runtime.Serialization -out:"$here/Harness.exe" "$here/UnityContract.cs" "$here/Dependencies.cs" "$here/Harness.cs" "$source/CampoDiscos.cs"
${MONO:-mono} "$here/Harness.exe"

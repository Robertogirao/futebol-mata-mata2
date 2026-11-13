#!/bin/bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project="$here/../../Projeto"
mapfile -t sources < <(rg --files "$project/Assets/FutebolDiscos/Scripts" "$project/Assets/Scripts" | rg '\.cs$' | sort)
${MCS:-mcs} -target:library -r:System.Runtime.Serialization -out:"$here/AllRuntime.dll" "$here/UnityContract.cs" "${sources[@]}"

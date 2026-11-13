#!/bin/bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project="$here/../../Projeto"
mapfile -t sources < <(rg --files "$project/Assets/FutebolDiscos/Scripts" "$project/Assets/Scripts" | rg '\.cs$' | sort)
${MCS:-mcs} -langversion:latest -define:UNITY_EDITOR -r:System.Runtime.Serialization -out:"$here/ActualHarness.exe" "$here/ActualRuntimeContract.cs" "$here/ActualEditorFacade.cs" "$here/ActualHarness.cs" "$project/Assets/FutebolDiscos/Editor/ProjetoFutebolDiscosEditor.cs" "$project/Assets/FutebolDiscos/Editor/PainelCentralDiscosEditor.cs" "$project/Assets/Editor/MataMataEditor.cs" "${sources[@]}"
${MONO:-mono} "$here/ActualHarness.exe" | tee "$here/actual-results.txt"

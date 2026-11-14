#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project="$here/../../Projeto"
${MCS:-mcs} -r:System.Core -out:"$here/Harness.exe" \
  "$here/Dependencies.cs" "$here/Harness.cs" \
  "$project/Assets/FutebolDiscos/Scripts/CapitaoConvidado.cs" \
  "$project/Assets/FutebolDiscos/Scripts/RepositorioCapitaesConvidados.cs" \
  "$project/Assets/FutebolDiscos/Scripts/MotorSorteioCapitaes.cs"
${MONO:-mono} "$here/Harness.exe" | tee "$here/results.txt"

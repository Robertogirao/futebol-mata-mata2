#!/bin/sh
set -eu
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project="$here/../../Projeto"
${MCS:-mcs} -r:System.Xml -out:"$here/OutsideHarness.exe" "$here/UnityStubs.cs" "$here/Dependencies.cs" "$here/Harness.cs" "$project/Assets/Scripts/TournamentManager.cs" "$project/Assets/Scripts/LigaCampeonato.cs" "$project/Assets/Scripts/UniversalUI.cs" "$project/Assets/Scripts/MataMataCampeonato.cs" "$project/Assets/Scripts/MataMataModelo.cs" "$project/Assets/Scripts/CalendarioCsvParser.cs"
${MONO:-mono} "$here/OutsideHarness.exe"

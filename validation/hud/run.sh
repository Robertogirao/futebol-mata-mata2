#!/bin/sh
set -eu
cd "$(dirname "$0")/../.."
${MCS:-mcs} -out:validation/hud/Harness.exe \
  validation/hud/UnityContract.cs \
  validation/hud/Dependencies.cs \
  validation/hud/Harness.cs \
  Projeto/Assets/FutebolDiscos/Scripts/HudFutebolDiscos.cs \
  Projeto/Assets/FutebolDiscos/Scripts/RoteiroTransmissaoDiscos.cs \
  Projeto/Assets/Scripts/LayoutJogadorUnicoUI.cs \
  Projeto/Assets/Scripts/GoalBannerUI.cs \
  Projeto/Assets/Scripts/TeamData.cs \
  Projeto/Assets/Scripts/ClassificacaoUI.cs \
  Projeto/Assets/Scripts/TemaCompeticao.cs
${MONO:-mono} validation/hud/Harness.exe

#!/usr/bin/env bash
set -euo pipefail
validation_dir="$(cd "$(dirname "$0")" && pwd)"
project_dir="${validation_dir}/../../Projeto"
compiler="${MCS:-mcs}"
runtime="${MONO:-mono}"
"${compiler}" -r:System.Runtime.Serialization -out:"${validation_dir}/CalendarTests.exe" \
  "${project_dir}/Assets/FutebolDiscos/Scripts/CalendarioDiscos.cs" \
  "${project_dir}/Assets/FutebolDiscos/Scripts/ConfiguracaoTemporadaDiscos.cs" \
  "${project_dir}/Assets/Scripts/CalendarioCsvParser.cs" \
  "${validation_dir}/CalendarTests.cs"
"${runtime}" "${validation_dir}/CalendarTests.exe" "${validation_dir}/generated" | tee "${validation_dir}/results.txt"

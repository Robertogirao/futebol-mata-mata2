#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
for suite in calendar field hud integration-outside runtime; do
  bash "$here/$suite/run.sh" | tee "$here/$suite/results.txt"
done
bash "$here/runtime/run-root.sh" | tee "$here/runtime/root-results.txt"
bash "$here/runtime/run-goal-reset.sh" | tee "$here/runtime/goal-reset-results.txt"
bash "$here/runtime/run-corner-restart.sh" | tee "$here/runtime/corner-restart-results.txt"
bash "$here/runtime/run-audio-selection.sh" | tee "$here/runtime/audio-selection-results.txt"
bash "$here/runtime/run-captain.sh" | tee "$here/runtime/captain-results.txt"
bash "$here/runtime/run-tactics.sh" | tee "$here/runtime/tactics-results.txt"
bash "$here/presentation/run.sh"
bash "$here/editor/run-actual.sh"
python3 "$here/scene/unity_asset_refs.py" "$here/../Projeto" --output "$here/scene/asset-reference-report.json"
python3 "$here/scene/validate_scene_contract.py"

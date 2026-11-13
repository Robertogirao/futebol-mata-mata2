#!/usr/bin/env bash
set -euo pipefail
here=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
for suite in calendar field hud integration-outside runtime; do
  bash "$here/$suite/run.sh" | tee "$here/$suite/results.txt"
done
bash "$here/runtime/run-root.sh" | tee "$here/runtime/root-results.txt"
bash "$here/runtime/run-goal-reset.sh" | tee "$here/runtime/goal-reset-results.txt"
bash "$here/runtime/run-captain.sh" | tee "$here/runtime/captain-results.txt"
bash "$here/editor/run-actual.sh"
python3 "$here/scene/unity_asset_refs.py" "$here/../Projeto" --output "$here/scene/asset-reference-report.json"
python3 "$here/scene/validate_scene_contract.py"

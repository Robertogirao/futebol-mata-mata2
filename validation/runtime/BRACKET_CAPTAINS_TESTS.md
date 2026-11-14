# Saved captain portraits in the knockout bracket

Run `bash validation/runtime/run-bracket-captains.sh`; select the compiler and
runtime with `MCS` and `MONO` if needed.

The suite executes the actual bracket UI, team assets, knockout model and
captain-assignment repository through the existing Unity UI/TMP API facade.
Its 2,553 passing checks cover:

- Saved assignments for brackets of 2, 4, 8, 16 and 32 clubs.
- The assigned portrait beside the club crest, name and score.
- Separate portrait/name/score geometry in dense bracket rows.
- Preview portraits, advancing clubs and the champion's main/summary portraits.
- Exact assignment-file bytes and the same captain after reopening a tournament.
- No reroll of football representatives or mutation of team assets/rosters.
- Original layout when the provider or a captain's photo is missing.
- Empty TBD/BYE slots without guessed portraits.
- Cosmetic photo updates without assigning a different captain.

The provider reads the captain already associated with a team. Rendering a
bracket never selects another captain or changes the saved draw.

This suite does not render fonts/images or run the native Unity layout engine.
Verify final appearance with the project's actual fonts in the Unity Editor.

# Captain-draw view

Run `bash validation/draw-ui/run.sh`, or use `validation/run-all.sh`.
Select the compiler/runtime with `MCS` and `MONO` if needed.

The suite compiles the actual runtime and exercises the real `SorteioCapitaesUI`
and marble simulation with Unity UI/TMP API facades. Its 591 passing checks cover:

- All selected marbles and club cards for 2, 10, 16, 20 and 32 participants.
- Circular portrait masks, canvas ordering and initial position bounds.
- Separate readable text rows on the dense 32-club board.
- Exact simulation assignments, counters and completed portraits.
- Every rendered frame of the arrival queue and assignment flights for 10, 16,
  20 and 32 captains: finished marbles stay outside the obstacle chamber.
- Slow-reveal backlogs: arrival thumbnails remain separated, inside the gate
  width and above its caption, and every final assignment still completes.
- Recorder clocks with `timeScale = 0` and unscaled fallback.
- Saved replay, cancellation, rebuilding, hiding and component disable.
- Generated circle resources and preservation of external portraits/crests.

The suite does not render fonts, run the native Unity import pipeline or model
native mask/stencil rendering. Verify the final appearance in the Unity Editor.

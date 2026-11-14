# On-screen captain draw integration

Run `bash validation/runtime/run-captain-draw.sh`, or use `validation/run-all.sh`.
`MCS` and `MONO` can select alternative compiler/runtime executables.

The harness compiles the actual controller, deterministic draw simulation,
Canvas construction, captain repository and tournament engines. It verifies:

- League and knockout start only after all captain reveals and the final board.
- The persisted outcome is exactly the live obstacle simulation's result, with
  one unique captain per club and fixed manual associations respected.
- Capture time advances the draw while `Time.timeScale` is zero, without
  advancing the football clock or preparing match actors/results prematurely.
- There are 10, 16, 20 or 32 actual captain-marble views and club cards according
  to active participants, even when the catalogue contains extra captains.
- Cancelling during warmup or after a partial reveal writes no assignment file
  and cannot start a match later. A subsequent Play uses the saved season seed.
- A complete outcome is saved before the final summary; stopping during that
  summary keeps the pairing and the next Play skips a redundant draw.
- Existing outcomes are skipped by default. Requested saved replays are labeled
  and reveal the original mapping without rewriting any snapshot bytes.
- An exception inside a nested presentation iterator and a conflicting/corrupt
  assignment file use normal failure cleanup, preserving files and preventing
  a half-started competition.
- Missing captain metadata over existing results fails closed. Ordinary football
  scenes and display-off guest scenes retain their previous behavior.

The suite passes 118 assertions. The custom deterministic obstacle simulation executes during these tests.
Unity APIs, coroutine timing and Canvas components are represented by the
existing contract facade; native Unity rendering, input, audio and imports are
not executed. The older guest-captain integration fixture explicitly disables
the optional draw so its existing identity/strength/save checks remain focused.

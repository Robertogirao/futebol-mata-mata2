# Marble draw engine

Run `bash validation/captain-draw-engine/run.sh`; `MCS` and `MONO` can select alternate executables.

The runner compiles the actual `MotorSorteioCapitaes`, captain asset and repository validator. A small Vector2/data contract replaces the Unity types; collision and assignment logic are the real production implementation.

The 1,116 assertions cover:

- 2, 10, 16, 20 and 32 active clubs, surplus captain catalogues and five seeds, including both integer extremes.
- All selected marbles visible initially, no fresh destinations assigned before arrival, and one captain per club at completion.
- Identical selection, arrival order and assignment at 30 and 60 frames per second, independent of catalogue ordering.
- Circle/capsule contacts with pegs, ramps and moving obstacles, visible collision response and finite horizontal bounds.
- A direct counterexample to a staged preassignment: changing the first physical arrival changes the first club pairing and final mapping.
- Fixed captain reservations, equivalent club ID references, legacy saved-draw replay without mutation or reroll, and neutral overall during the draw.
- New seeds produce distinct pairings. Completed engines cannot duplicate reveals or alter results.
- A final lane opening releases even deliberately obstructed marbles. Every result still requires a physical finish crossing; there is no timer-based assignment of unfinished marbles.
- Invalid catalogues, duplicate manual bindings, malformed replay lists and non-finite/negative time are rejected.

The simulator uses its own deterministic 120 Hz 2D collisions. These tests do not execute Unity rendering, imported sprites or native football physics.

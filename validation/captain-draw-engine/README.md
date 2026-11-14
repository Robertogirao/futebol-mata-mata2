# Marble draw engine

Run `bash validation/captain-draw-engine/run.sh`; `MCS` and `MONO` can select alternate executables.

The runner compiles the actual `MotorSorteioCapitaes`, captain asset and repository validator. A small Vector2/data contract replaces the Unity types; collision and assignment logic are the real production implementation.

The 1,179 assertions cover:

- 2, 10, 16, 20 and 32 active clubs, surplus captain catalogues and five seeds, including both integer extremes.
- All selected marbles visible initially, no fresh destinations assigned before arrival, and one captain per club at completion.
- Identical selection, arrival order and assignment at 30 and 60 frames per second, independent of catalogue ordering.
- Circle/capsule contacts with pegs, ramps and moving obstacles, visible collision response and finite horizontal bounds.
- A crowded pair cannot push a marble into a pin after the obstacle solver; the old source fails this adversarial regression.
- Swept circle/capsule checks catch a grazing path between integration endpoints and pair-separation corrections.
- Dense piles of 32 marbles and every step of normal races retain clearance from visible solid obstacles.
- Rotor clearances accommodate a complete marble throughout the full rotation, not only at the initial angle.
- A direct counterexample to a staged preassignment: changing the first physical arrival changes the first club pairing and final mapping.
- Fixed captain reservations, equivalent club ID references, legacy saved-draw replay without mutation or reroll, and neutral overall during the draw.
- New seeds produce distinct pairings. Completed engines cannot duplicate reveals or alter results.
- A local velocity release frees sustained balancing without teleporting, assigning a club prematurely or removing pins. Obstacles remain solid in the final phase.
- Invalid catalogues, duplicate manual bindings, malformed replay lists and non-finite/negative time are rejected.

The simulator uses its own deterministic 120 Hz 2D collisions with four collision substeps and iterative contact reconciliation. These tests do not execute Unity rendering, imported sprites or native football physics.

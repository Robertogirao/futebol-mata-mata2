# Procedural race course model

`RaceCourse.cs` is independent of Unity. It contains serializable points, course blocks, obstacle definitions and one optional bypass, plus deterministic generation and arc-length geometry helpers.

The same seed reconstructs the same geometry, block order, terrain, obstacle settings and bypass. A custom seeded PRNG avoids relying on `System.Random` implementation details. Courses contain three connected rows, with curvature varied inside the blocks and sampled U turns. Main centerlines remain within x [-10,12], y [-4.3,4.3]. A default course has nine configurable blocks plus the two turns. The first 6.5 arc units remain free of obstacle centers for the largest starting grid.

Obstacles use world diameter or bar span in `size`; spinner `speed` is degrees per second, while other moving obstacles use a cycle multiplier. Static bumpers and animated gates are narrower than the road. Collision shape creation and animation belong to `RaceTrackView`, not this model.

Bypass waypoints join exact samples of the main course at `startDistance` and `endDistance`. A runner using that route must project onto the bypass polyline and map its local fraction back into the declared main-course distance interval. A bypass avoids obstacles; it is not guaranteed to be geometrically shorter than the main centerline.

`RaceCourse.Project` searches only two arc units behind and `maxAdvance` ahead of the prior cursor, and rejects positions too far from that local road. This prevents a marble on another row from gradually advancing a cursor to the finish. `RacePolyline.Project` provides the local arc window without the course-width check, for branch geometry.

## Verification

Run `./run.sh` in this folder. The runner uses the existing local Mono compiler/runtime under `/workspace/work/validation` and compiles the actual source unchanged together with `Harness.cs`.

107,459 checks passed across 750 generated courses (50 seeds, five block counts and three widths), with additional boundary seeds and invalid/corrupt geometry cases. See `results.txt` and `report.json`.

This is pure C# domain and geometry validation. No Unity Editor, asset import, rendering, actual collider contacts, motor/ability behavior or race completion timing was tested here.

# Stadium geometry validation

Run `bash validation/field/run.sh`.

The harness compiles the actual `Assets/FutebolDiscos/Scripts/CampoDiscos.cs`. Small dependency types isolate the field from controllers owned by other project modules. `UnityContract.cs` is a lifecycle/API facade, not the Unity engine and not a collision solver.

The 774 assertions cover the shared API, normalized circle sprite, field dimensions, 12 solid boundary colliders, four round goalposts, two recessed scoring triggers, clear central/offset goal entry lanes, closed net sides and backs, perimeter closure, shared materials, custom background normalization, English field text, independent goal glows, recording time, owned resource destruction, rebuilds and preservation of imported assets. The goal sensors begin at x±11.46; the match controller additionally requires the complete white ball to cross the goal line before registering a score.

Exterior-picture checks cover proportional cover sizing for wide, portrait and square images, an expanded orthographic viewport, opacity, optional stand/crowd overlays, layering below the opaque pitch and recessed goals, unchanged colliders, and preservation of supplied sprites and textures during rebuild and cleanup.

`geometry.json` records the generated box collider positions/sizes. `report.json` and `results.txt` contain the result. Native Unity rendering, prefab import and Box2D contacts still require verification in Unity 6.4.

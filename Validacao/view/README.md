# Course world validation

Run `./run.sh` from this directory in the preparation workspace. It compiles real `RaceCourse.cs`, `RaceTrackView.cs` and `RaceObstacleMotion.cs` with the copied `UnityContract.cs` facade, then runs `Harness.cs`.

The 150 checks cover six generated courses, all five obstacle kinds, real generated meshes and collider components, camera-sized coordinates, passable main and bypass centerlines, ghost collision lists, animation state, resources and cleanup. `results.txt` and `report.json` record the source hashes.

The facade models scene objects, sprite bounds, mesh buffers and kinematic motion API calls. It does not run a Unity renderer, physics solver, asset importer or native callback timing. It immediately executes disposal, whereas Unity's `Destroy` is deferred; `Clear` explicitly deactivates the owned root before requesting destruction.

No production source is patched for the test. The runtime compiler wrappers are `/workspace/work/validation/mcs-local` and `mono-local`.

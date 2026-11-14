# Guest-captain assignment store

Run `bash validation/guest-captains-store/run.sh`; `MCS` and `MONO` can select alternate compiler/runtime executables.

The harness compiles the actual `CapitaoConvidado` and `RepositorioCapitaesConvidados` sources. It uses a minimal Unity asset adapter and an injected XML serializer to exercise real filesystem reads, writes, locking and preservation without requiring native Unity import or JSON serialization.

Coverage includes 2–32 teams, unique captain assignments, seed stability, reordered catalogs/manual lists, random remainder with reserved manual captains, all-manual competitions, independent display/player data, individual-rating snapshots, extra/replacement mode identity, validation, corrupt metadata, missing assets, changed rating/seed/rules and missing companion metadata over existing results. Failed loads preserve saved bytes and unrelated season/result files. A serializer that changes valid assignments cannot commit them.

Completed obstacle-draw results are also exercised for 2, 10, 16, 20 and 32 teams. Arrival-order records are copied into canonical team order without generating another assignment or mutating the simulator's records. Partial/duplicate/missing/unknown teams, unknown or repeated captains, changed ratings and manual conflicts cannot create a file. Repeated identical completion reuses the original bytes and timestamp; a different valid completion, invalid existing file, missing companion over old results, serializer mutation or active writer lock cannot replace the stored draw. Version-one assignments created by the original loader remain compatible. The suite passes 874 assertions.

Asset names identify saved captain assets, matching the existing team-asset convention. Display name and photo can change cosmetically; assigned overall and the extra/replacement rule stay fixed for a season. The tests do not execute native Unity rendering, physics, asset import or `JsonUtility`.

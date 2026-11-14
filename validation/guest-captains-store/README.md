# Guest-captain assignment store

Run `bash validation/guest-captains-store/run.sh`; `MCS` and `MONO` can select alternate compiler/runtime executables.

The harness compiles the actual `CapitaoConvidado` and `RepositorioCapitaesConvidados` sources. It uses a minimal Unity asset adapter and an injected XML serializer to exercise real filesystem reads, writes, locking and preservation without requiring native Unity import or JSON serialization.

Coverage includes 2–32 teams, unique captain assignments, seed stability, reordered catalogs/manual lists, random remainder with reserved manual captains, all-manual competitions, independent display/player data, individual-rating snapshots, extra/replacement mode identity, validation, corrupt metadata, missing assets, changed rating/seed/rules and missing companion metadata over existing results. Failed loads preserve saved bytes and unrelated season/result files. A serializer that changes valid assignments cannot commit them.

Asset names identify saved captain assets, matching the existing team-asset convention. Display name and photo can change cosmetically; assigned overall and the extra/replacement rule stay fixed for a season. The tests do not execute native Unity rendering, physics, asset import or `JsonUtility`.

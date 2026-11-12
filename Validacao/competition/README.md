# Race competition and persistence validation

Run `./run.sh` in this workspace. It compiles the real `RaceCompetition.cs` and `RaceStorage.cs` with local Mono, then executes `Harness.cs`. The runner depends on the existing local wrappers in `/workspace/work/validation` and does not install software system-wide.

The 2,769 assertions cover:

- Single race and Mini GP with 2, 8 and 16 participants, 1/3/30 races, and fixed seed boundaries.
- Distinct per-race course seeds, deterministic schedules and immutable public snapshots.
- Automatic seed zero persisting the same effective seed on reload.
- Full actual-JSON round trips after every GP race, points once per finish, idempotent retries and completed championships never replaying.
- Best of Three concluding at two wins, both straight wins and a deciding third race.
- Knockout sizes 2 through 32, balanced heats of up to eight, top-half qualifiers, no eliminated participant returning and the final champion ranking first.
- Invalid IDs, duplicate participants, format/count/time/size limits, changed gameplay configuration, corrupt schedule metadata and invalid finish records.
- Cosmetic title changes preserving a season, while changes to roster/order, format, seed, track version, race count, durations, type powers or marble size require a new season.
- Actual filesystem writes, failed serializer round trips, a real `File.Replace` backup-path failure, no live-model advancement on failure, safe retry and temporary-file cleanup.
- Exclusive file locks and stale writers preserving the current championship.
- Restoring the previous durable backup when the main file is corrupt or missing, retaining original corrupt bytes, and refusing to reset when both saved files are unreadable.
- Optional standings CSV export with English headers.

## Runtime API

`RaceStorage.LoadOrCreate(config)` prepares or resumes a championship. Runtime code must call `RaceStorage.SaveResult(competition,result)` rather than directly changing the domain model. The storage method writes a validated candidate atomically, then updates the live competition. A write failure therefore leaves the current race pending.

`RaceCompetition.CompleteRace(result)` exists for pure in-memory modeling and validation; it does not write files. `CurrentRace`, `Config` and `Snapshot()` return copies.

A new championship persists all schedule slots and course seeds immediately. Knockout opponents in later slots are resolved and saved as each previous stage finishes. Best of Three keeps the maximum three planned seeds but stops at the second win. A stopped in-progress race restarts from the beginning using its saved plan; completed races retain their results.

Points for GP/single/knockout places 1–8 are **10, 8, 6, 5, 4, 3, 2, 1**, and later places get zero. Best of Three uses one point per race win and ends at two wins. GP ties use wins, sum of finish positions, cumulative time and original roster order. Knockout standings prioritize the highest qualified stage and its finish place before points, so a final winner always leads eliminated participants.

## Save files

- `championship.json`: current durable state.
- `championship.backup.json`: state before the latest successful replacement.
- `championship.lock`: OS file-share lock; an empty file can remain after use.
- `championship.corrupt.<id>.json`: original unreadable main file retained during successful recovery.
- `standings.csv`: generated only when the optional export method is called.

Backup recovery may repeat the most recently saved race because the backup represents the previous durable state. `RecoveredFromBackup` and `RecoveryMessage` expose that situation. A valid main file with a configuration mismatch is never silently replaced by an older backup. Results have to be reloaded if another session advanced the championship.

## Scope

These are tests of actual C# domain and filesystem code, without Unity stubs. The injected JSON adapter uses `DataContractJsonSerializer`. It does not certify Unity `JsonUtility`, Unity lifecycle timing, track geometry, rendering, launch behavior, collision physics or race finishes in the Unity Editor. All serialized DTOs use public fields and `[Serializable]` for the runtime adapter.

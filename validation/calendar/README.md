# Calendar and immutable season metadata validation

Run `bash run.sh` from this folder. This compiles the actual new domain sources together with the existing project's `CalendarioCsvParser.cs`, then exercises real temporary files under `generated/`.

Result: **37,629 passing assertions**. The harness checks:

- Every roster size from 2 to 32: one game per team per round; all unordered pairs occur exactly once per complete turn; odd rosters have exactly one bye per team per turn; return fixtures swap sides exactly.
- Arbitrary schedules through 100 rounds, including third and later turns; deterministic seeded shuffle and unchanged source roster order.
- Exact CSV round trip through the real existing parser, with accented asset names, spaces, commas, semicolons and escaped double quotes.
- Invalid rosters, round counts, settings, corrupt fixtures and missing byes are rejected.
- A random effective seed is generated once for a saved season; fixed signed seeds are preserved; loading preserves file bytes and schedule.
- Every public immutable configuration field is changed independently and rejected without overwriting saved metadata; a new season keeps the old file.
- Incomplete/corrupt JSON, a valid calendar belonging to different settings, serializer failure, lock contention and failed atomic rename preserve existing data and allow safe retries.
- Existing results without their matching season metadata cannot trigger a new seed.

The serializer is injected through `ISerializadorTemporadaDiscos`. These tests use .NET `DataContractJsonSerializer`, rather than reimplementing storage in a fake. The Unity integration supplies its own `JsonUtility` adapter. No Unity runtime or Editor was available here; this suite validates the pure scheduling/persistence sources and the existing CSV parser, not Unity serialization or scene behavior.

`ConfiguracaoTemporadaDiscos.cs` contains only identity and game rules. Names, fonts, backgrounds and presentation timing remain editable because they are absent from the immutable configuration DTO. `CarregarOuCriar(config, possuiResultadosExistentes)` must receive `true` when result history exists. Store the metadata below `Application.persistentDataPath/Discos/<competition>/<season>`; its filename is `configuracao_discos.json`.

Both source files are independent of Unity and never modify source `TeamData` assets. They retain raw asset IDs in the roster and CSV. Safe path restrictions apply only to the competition/season folder IDs.

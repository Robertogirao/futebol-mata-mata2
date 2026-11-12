# Race HUD validation

Run `./run.sh` in this workspace. It compiles the actual RaceHud.cs and executes
232 assertions using UnityEngine/UI/TMP test doubles. The local Mono wrappers
are at `/workspace/work/validation/mcs-local` and `mono-local`.

The new UI is built explicitly, without relying on cross-component Awake or
OnEnable ordering. Tests cover 8/16 entrants, font selection, standings,
countdown, timer, ability notices, pooling over repeated live updates and cleanup.

No Unity Editor was run. Geometry assertions do not prove actual font rendering,
real CanvasScaler behavior on non-16:9 screens or imported fonts. The product
layout is designed for 1920×1080 (16:9), matching the course presentation.

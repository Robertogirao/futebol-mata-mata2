# Outside-field integration validation

Run `sh run.sh` from any directory. This compiles the actual TournamentManager, LigaCampeonato, UniversalUI, MataMataCampeonato, MataMataModelo and CalendarioCsvParser C# sources.

The harness checks recording-aware regulation time, reset pauses, draw vs. real golden-goal behavior, physics preparation before player cards, clock before physics start, a shared representative selection, and child HUD Canvas discovery. It also checks league and knockout durable state across reload, pending-result recovery after actual filesystem failures, duplicate callbacks, final champions, completion gates, copy isolation, and next-round lookup.

Unity is unavailable in this environment. The facade models coroutine scheduling and view/physics calls for control-flow checks; its JsonUtility uses XML serialization to exercise real filesystem transactions. These checks do not certify Unity JSON import, actual contacts, or rendering. The full project validation separately checks the Unity 6.4 API contract.

# Anti-stick state and API regressions

Run with the .NET 8 SDK from the repository root:

```sh
dotnet run --project Tests/AntiStick/AntiStick.csproj
```

The project compiles the actual `DesprenderBolaDaHelice.cs` source against small deterministic Unity API stubs. It has no external NuGet dependencies.

Coverage includes co-rotation with interrupted callbacks, relative-motion progress, the three-second jitter retention cap, proximity gating, mass-independent escape velocity, scoped compound-collider ignore leases, preexisting ignores, and cleanup on timeout, freeze, disable, destruction, scoring, and application pause. The release does not move the ball or change its identity.

These tests validate component state transitions and API calls. They are **not Unity physics, import, rendering, or Play Mode tests**. Use the Unity editor to validate the actual scene and contacts.

# Trapped-corner restarts

Run `bash validation/runtime/run-corner-restart.sh`, or use `validation/run-all.sh`.
The compiler/runtime can be selected with `MCS` and `MONO`, as in the other suites.

The harness compiles the actual match, ball, clock, field, HUD and goal sources.
It injects ball positions, collision callbacks and video/physics steps to verify:

- All four corners and either team's last contact, in league and knockout configurations.
- Defender touch gives an attacking corner; attacker touch gives a defensive goal kick.
- Short visits, useful progress, center/side stagnation, absent contact and disabled feature do not award a restart.
- Local shaking, high instantaneous velocity and changing last contact do not conceal a sustained trap.
- Real `OnCollisionStay2D` contact tracking works independently of shot cooldown.
- Legacy rescue impulses do not compete with the corner timer, including Golden Goal.
- Placement leaves collision clearance at maximum ball/disc sizes and with five discs per team.
- Physical/visible poses agree, clock pauses during the announcement, possession is retained and the inward launch is speed bounded.
- Stale goal poses and repeated callbacks cannot score until consecutive restart poses are confirmed.
- Stopping cancels pending actions; a fresh kickoff clears the old corner state.
- A legitimate later goal still counts, including normal Golden Goal completion.

The suite passes 654 assertions. It exercises lifecycle/control logic through the existing Unity façade; native Unity collision response and rendering are not executed.

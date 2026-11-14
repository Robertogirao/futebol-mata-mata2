# Guest captains in the real competition flow

Run `bash validation/runtime/run-guest-captains.sh`, or run the aggregate `validation/run-all.sh`.
`MCS` and `MONO` select alternative compiler/runtime executables.

The harness compiles and runs the actual controller, competition managers, repositories,
match, actors, scoreboard, captain cards and scorer banners. It checks:

- League and knockout assignment before the first fixture, manual bindings and one unique guest per club.
- Extra-captain and replacement-slot layouts, with the first footballer retained in ordinary slots.
- The same guest object supplies the opening portrait, on-field captain, name tag and goal banner.
- Footballer goals retain their own names and club crest; football roster assets remain unchanged.
- Changing only the captain's overall changes his movement, shot power and same-seed shot accuracy;
  teammate performance remains identical, both with team-overall effects enabled and disabled.
- Five footballers plus a guest fit into six formation slots per side; absent guest photographs use the club crest.
- Completed match results and captain assignments survive a fresh Play, with the next scheduled fixture using
  the same club/captain mapping and no repeated pending-round score.
- Assigned-rating, extra-slot and guest-mode changes cannot silently rewrite a saved season.
- A new season has an independent assignment store, while original football scenes require no guest catalogue.

The suite passes 222 assertions. The adapters inject lifecycle, clock, pose and goal steps;
native Unity contact solving, rendering and audio output are not executed.

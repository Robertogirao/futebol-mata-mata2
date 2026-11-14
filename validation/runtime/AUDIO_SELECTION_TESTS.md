# Selected match effects

Run `bash validation/runtime/run-audio-selection.sh`, or `validation/run-all.sh`.
`MCS` and `MONO` can select alternate compiler/runtime binaries.

The harness compiles the actual UniversalUI and project sources, then records the
AudioClip reference and volume passed to each facade AudioSource.PlayOneShot call.
It passes 47 assertions covering:

- Selected kickoff, goal and final effects in league and knockout configurations.
- An automatic dedicated effects source instead of adopting an existing source.
- Preservation of independent background audio and its assigned clip/settings.
- An explicit source with an old clip already playing: preparation stops that playback and clears its embedded clip.
- Changing, destroying or clearing the effects source assignment between events.
- Changed clips in the next match, rather than retained previous selections.
- Empty clip fields intentionally mute their events without an old-clip fallback.
- Configured one-shot volume and supported volume bounds.
- Golden Goal entry, goal/final effect order and protection against duplicate callbacks.
- Cancellation, component disable and destruction stop/dispose owned effects without stopping independent audio.

This verifies source ownership and exact clip dispatch, not the reported user's
custom scene state or the cause of an audible mismatch in that scene. The facade
does not decode or output audio. Native Unity playback must be checked in the Editor.

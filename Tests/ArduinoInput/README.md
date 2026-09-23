# Re-run the Arduino Input System Play-mode checks

These files are outside Assets so test-only UnityEditor code does not ship in the game.
`Run.ps1` creates an isolated project under Temp/ArduinoValidation, copies the current player
scripts and Input Actions asset, and opens Unity in headless Play mode. It does not open real COM
ports: simulated snapshots feed the actual manager, custom devices, actions and PlayerInputs.
The main project and its scene state are not changed by the test run.

Run from PowerShell:

    ./Tests/ArduinoInput/Run.ps1

The test exits Unity with code 0 on success or 1 on failure. Read
Temp/ArduinoValidation/arduino-play-checks-passed.txt and checks.log for the result.
Unity must be installed at the normal Unity Hub path for this project's editor version.

Coverage: no enabled slots, one and four Arduinos, player enabled before device exists,
exclusive pairing, movement/fire isolation, stale release, unaffected other players, disabled
slot cleanup, duplicate ports/players, repaired assignment, keyboard restoration, calibration,
axis inversion/swap, old/new serial packet validation and manager shutdown.

Tests explicitly select gameplay input updates because an unfocused headless editor normally
routes updates into editor input state. This setting is confined to the temporary test project.
Physical USB disconnect/reconnect and four real boards still need a hardware playtest.
Three-button coverage: every button combination, legacy packet compatibility, malformed packets, per-player extra button isolation, release and timeout.

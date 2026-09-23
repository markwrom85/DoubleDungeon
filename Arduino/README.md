# Arduino controllers through Unity Input Actions

One `ardunoManager` supports zero to four enabled serial connections. Each enabled slot maps one
Arduino to one explicitly assigned scene `PlayerInput`. Keyboard/mouse and gamepad players use
the same movement and shooting actions. Upload the updated sketch to send all three buttons.

## Wiring and upload (Uno R3 / classic Nano)

Unplug USB before wiring a standard analog joystick:

| Joystick | Arduino |
| --- | --- |
| VCC / +5V | 5V |
| GND | GND |
| VRx | A0 |
| VRy | A1 |
| SW / fire button | D2 |
| Second button | D3 to GND |
| Third button (future SwitchSide) | D4 to GND |

A separate normally-open pushbutton can instead connect D2 to GND; the sketch uses INPUT_PULLUP.
For a 3.3V board, verify voltage compatibility first; do not feed 5V analog outputs into 3.3V inputs.

Open `Arduino/JoystickController/JoystickController.ino`, select the board and COM port, then upload.
Serial Monitor at **115200 baud** should show `512,512,0,0,0` near center; the third number becomes 1
while pressing fire. Close Serial Monitor/Plotter before Unity opens the port.
Messages are newline-terminated `x,y,fire,button2,switchSide`. All buttons send 1 pressed and 0 released; D2, D3 and D4 use INPUT_PULLUP and connect to GND when pressed. Older messages containing two, three or four fields remain accepted; missing buttons are released.

## Set up the manager

1. Stop Play and let Unity import/compile.
2. In the gameplay scene choose **Double Dungeon > Set Up Arduino Manager**.
   This creates/selects the manager and enables desktop .NET Framework serial support.
3. Configure only the slots you need. Enable the slot, enter its actual COM port, and drag the
   intended **scene player's PlayerInput component** into Player. Do not assign a prefab asset.
   If the scene has exactly one player, the setup menu assigns it to slot 1 and starts with COM9
   (the last saved prototype port); verify that number against Arduino IDE.
4. Leave unused slots disabled. Every enabled slot must have a different port and player.
5. Save the scene, then press Play. Your existing gameplay system must set `isPlayable = true`.
6. Inspect the manager during Play for individual connection and pairing status. Use
   **Reconnect all Arduinos** after plugging a board back in or fixing a connection failure.
7. To calibrate, release the stick and click that slot's **Calibrate** button. Play-mode edits are
   temporary; copy the values and apply them after stopping to retain calibration.

Each slot has its own center, dead zone, axis swapping, and axis inversion. These settings and
COM ports now belong to the manager, not ArduinoJoystickPlayer. Movement speed stays on the player.
Existing scene/prefab overrides for the removed serial fields are no longer used.

An enabled, assigned Arduino slot selects that player's **Arduino** control scheme exclusively.
Disable the slot to return to its previous available keyboard/gamepad scheme. Other players'
controller choices are unaffected. Missing serial data releases movement and all buttons after 0.5 seconds;
reconnecting requires the Reconnect button (there is no automatic port detection or reconnect loop).
A missing board does not block other slots. Ports are opened only from explicitly enabled slots.

For players spawned by the lobby, call `manager.AssignPlayer(slotIndex, playerInput)` after spawn;
slotIndex is 0–3. You can also assign the spawned scene PlayerInput in the Inspector during Play
for testing. Alternatively, leave Player empty and enable Press Fire To Join: D2 asks the active PlayerInputManager to spawn and pair a player when joining is enabled. Release D2 before firing. DungeonManager is unchanged.
Keep the manager in the same gameplay scene as its assigned players; scene unload closes its ports.

## Input flow

Arduino -> serial worker -> latest validated sample -> calibration -> ArduinoInputDevice ->
PlayerInput's **Player/Move** and **Player/Attack** actions -> player physics / gun.

- `ardunoManager.cs`: slot ownership, pairing, calibration, status, stale-input reset and reconnect.
- `ArduinoSerialConnection.cs`: one background reader per port; no Unity API calls on workers.
- `ArduinoInputDevice.cs`: the custom Input System layout (`move` Vector2 and `fire`, `button2`, `switchSide` buttons).
- `ArduinoJoystickPlayer.cs`: reads actions only; Rigidbody2D movement and the existing isPlayable gate.
- `DesktopMovementInput.cs`: reads the player's actions for all devices, despite its historical name.

Bindings live in `Assets/Settings/InputSystem_Actions.inputactions`. Its Arduino scheme binds
`<ArduinoInputDevice>/move` to Move and `<ArduinoInputDevice>/fire` to Attack. The manager never
creates gameplay bindings in code. PlayerInput must use that asset (or an equivalent asset with
these bindings, actions, and scheme). Keyboard/gamepad/mouse bindings are retained.

`Release When Unfocused` defaults to true. Leave it enabled for normal play; disabling it is for
background input diagnostics and does not override the player's separate focus gate.

## Movement and shooting

WASD/arrows or a gamepad stick/D-pad feed Move. Hold right mouse to move toward the cursor.
Hold left mouse, Space, Enter, right trigger, or the gamepad west button to feed Attack.
Arduino input uses the same actions after pairing, with no serial fallback in the player.

Four 90-degree movement sectors steer the gun. Hold fire to lock its current direction while
moving freely; release to resume steering. Standing still preserves facing. Exact diagonal ties
keep a bordering previous facing, or choose horizontal if neither bordering direction matches.

Current prefabs are in `Assets/Prefabs/Player`: Player, Wand, and SpellProjectile. The gun uses
6 shots/second; projectiles default to speed 12 and lifetime 2 seconds. Each gun pools 24 bullets,
growing to at most 128. Bullets return on expiry or collision with an eligible non-trigger 2D collider.
There is no enemy damage logic yet. Original prototype creation menus still refer to the older
prefab locations; use the new **Set Up Arduino Manager** menu for this migration.

## Troubleshooting

- Access denied: close Serial Monitor/Plotter and other programs using the port.
- No recent data: check baud, sketch, USB cable and wiring, then reconnect.
- No movement: confirm enabled slot, correct scene PlayerInput, Arduino scheme/bindings and isPlayable.
- Pairing error: fix PlayerInput's actions asset, then reconnect.
- Duplicate port/player: give every enabled slot a unique assignment.
- Namespace Ports missing: desktop Player Settings > API Compatibility Level > .NET Framework.
- Always firing: verify D2 wiring and that the transmitted third number changes between 0 and 1.

Physical multi-board testing is still required. The automated checks use simulated serial snapshots
and actual Unity Input System devices/PlayerInputs in an isolated Play-mode project.

## Reserved buttons

D3 feeds Player/Button2 and D4 feeds Player/SwitchSide through each paired player's Input Actions. These actions currently have no gameplay behavior. Screen switching can be connected to SwitchSide later. D2 still feeds Attack.

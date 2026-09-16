# First milestone: move a square with an Arduino joystick

The joystick sends movement and fire over USB: `512,512,0` followed by a newline.
Unity reads them at 115200 baud and moves the player in the XY plane.
This is a Windows/Mac/Linux desktop prototype, not a WebGL or mobile controller.

## 1. Wire your joystick

For an **Uno R3 or classic Nano and a standard analog joystick module**, unplug USB first:

| Joystick pin | Arduino pin |
| --- | --- |
| GND | GND |
| +5V / VCC | 5V |
| VRx | A0 |
| VRy | A1 |
| SW | D2 (optional fire button) |

For a 3.3V board, confirm the module supports 3.3V and power it from 3.3V;
do not feed 5V joystick outputs into 3.3V analog inputs. Confirm your board
model before choosing power. The sketch and Unity parser expect 10-bit readings (0–1023).

## 2. Upload the Arduino sketch

1. Open `Arduino/JoystickController/JoystickController.ino` in Arduino IDE.
2. Connect the Arduino with a USB data cable.
3. Select your board and its port under Tools. Write down the COM port.
4. Upload. Open Serial Monitor at **115200 baud**.
5. Expect lines near `512,512,0` when released; the first two numbers change with movement and the last becomes 1 while pressing the stick.
6. **Close Serial Monitor and Serial Plotter** so Unity can open the port.

## 3. Set up Unity

1. Open `Assets/Scenes/SampleScene.unity` and let Unity finish compiling.
2. Choose **Double Dungeon > Set Up Joystick Demo** from the top menu.
   This creates Player and enables the desktop .NET Framework API needed for serial access.
3. Wait for any recompilation. Select **Player** in the Hierarchy.
4. Set **Port Name** to the port from Arduino IDE (do not assume COM3 is correct).
5. Save the scene and press Play. A green square appears; allow a couple of seconds for the Arduino to reset.
6. Release the stick and click **Calibrate center** in the Game view if necessary.
7. Move the stick! Use **Invert X**, **Invert Y**, or **Swap Axes** to match its physical orientation.

Speed controls movement; Dead Zone prevents small resting fluctuations from moving the player.
The square stays inside the camera view. Calibration/Inspector changes made during Play are temporary;
copy the center values and set them again after stopping to retain them.
This first version moves the Transform directly and does not implement physics or collisions.

## Keyboard, mouse and standard controllers

The existing Player automatically supports all these movement inputs; no scene rebuild is needed:

| Device | Movement |
| --- | --- |
| Keyboard | WASD or arrow keys |
| Mouse | Hold right button to move toward the cursor; release to stop |
| Gamepad recognized by Unity | Left stick or D-pad |
| Arduino | Existing analog joystick |

Click the Game view to focus it. Keyboard/gamepad movement takes priority over mouse movement,
then Arduino. Inputs are not added together, so multiple devices and diagonals do not boost speed.
The overlay shows the active source. Arduino inversion and calibration only affect the Arduino.
Mouse movement ignores the status panel and stops at the cursor. It can be disabled with Mouse Movement.
Standard gamepads can connect during Play. Controller support depends on the device being
recognized as a Gamepad by Unity's Input System and the operating system.
For testing without the Uno, turn off Use Arduino before pressing Play; desktop controls also
work if opening the Arduino port fails or it is unplugged. Stop/Play after changing Use Arduino.
Shooting uses the Player/Attack action. Hold left mouse, Space, Enter, gamepad right trigger,
or the gamepad west face button (X on Xbox / Square on PlayStation) to fire.

### Editing the controls visually

Open `Assets/Settings/InputSystem_Actions.inputactions` in Unity and select the **Player** action map:

- **Move**: WASD, arrow keys, gamepad left stick and D-pad (plus the template's joystick/XR bindings).
- **MouseMove**: the button held for mouse movement, currently right mouse button.
- **Pointer**: mouse screen position used as the movement target.

Save the asset, then stop and start Play to use edited bindings. This asset is already configured
as the project's Input Actions asset. The player reads a private runtime copy via `InputSystem.actions`;
there are no movement bindings defined in C# and no PlayerInput component is required.
The existing Keyboard&Mouse and Gamepad control schemes remain available in the asset.
All connected devices can currently control the same player; per-player device pairing and an
in-game rebinding menu are not implemented. Arduino serial input still feeds movement separately.

## Player, Gun and Bullet prefabs

`Assets/Prefabs/Player.prefab` contains movement and a nested `Gun.prefab` instance.
The gun has a Barrel and Muzzle, and references `Bullet.prefab`. Drag **Player** into a scene
with an orthographic Main Camera. Disable/remove your old test Player first to avoid duplicate
players and COM-port conflicts. Set Port Name and any Arduino calibration/inversion on the new instance.
Alternatively, run **Double Dungeon > Set Up Joystick Demo** to add the Gun to an existing test Player,
preserving its settings. Save the scene afterward.

The gun follows movement using four 90-degree sectors. Exact diagonal ties retain a bordering
previous facing (otherwise choose the horizontal direction). Standing still keeps the last facing.
Holding fire locks the current facing and fires at 6 shots/second while movement remains free.
Release fire to follow movement again. The starting direction is Right.
Edit Shots Per Second on Gun; edit Speed and Lifetime on Bullet. Bullets return to their pool after 2 seconds
or when their trigger hits a non-trigger 2D collider outside the shooter. Enemy damage is not implemented.
The player still uses direct movement without wall collision handling.

For Arduino firing, unplug USB, connect joystick **SW to D2**, reconnect and upload the updated sketch.
The protocol is now `x,y,fire` (fire 0 or 1); the original two-number sketch remains supported for movement.
You can use keyboard/mouse/gamepad fire while moving with the Arduino without updating its sketch.

Test the gun facing each way, diagonal sector crossings, hold-fire strafing, release-to-turn,
and firing while stationary. Only one Player is intended for this prototype.

### Bullet pooling

Each gun prewarms 24 bullets under a separate **Bullet Pool** object during Play. Fired bullets
stay in world space while the player moves. Hits and lifetime expiry deactivate and return them;
the next shot resets position, rotation, velocity, shooter and lifetime before reusing them.
Edit **Initial Pool Size** and **Max Pool Size** on Gun before Play. The pool grows when empty,
up to 128 bullets by default; at the limit a shot is skipped until a bullet becomes available.
Disabling the gun returns its active bullets; destroying the gun cleans up its whole pool.
No prefab replacement or scene setup is required for existing guns.

Test: try each input independently, diagonal keyboard movement, switching between inputs,
unplugging the Uno during keyboard play, reconnecting a gamepad, and releasing the mouse at its target.

## Troubleshooting

- **Access denied:** close Serial Monitor/Plotter and any other app using the port.
- **Port missing:** check the USB data cable and Tools > Port; set Port Name again, then stop/Play.
- **Connected but no recent data:** verify the sketch, baud rate and newline output in Serial Monitor (stop Unity first).
- **Square drifts:** release stick, calibrate, then increase Dead Zone slightly if needed.
- **Wrong direction:** use the inversion/swap checkboxes.
- **Ports namespace missing:** use Edit > Project Settings > Player > Other Settings > Configuration > API Compatibility Level > .NET Framework for the desktop target.
- **Unplugged/replugged:** movement stops after half a second without valid samples; stop/Play to reconnect.

References: [Arduino analogRead](https://docs.arduino.cc/language-reference/en/functions/analog-io/analogRead/)
and [Unity .NET profiles](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html).

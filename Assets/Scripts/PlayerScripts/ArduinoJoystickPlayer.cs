using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
using System.IO.Ports;
using System.Threading;
#endif

// First prototype: direct movement on the XY plane, without collisions.
[RequireComponent(typeof(Rigidbody2D))]
public class ArduinoJoystickPlayer : MonoBehaviour
{
    public string portName = "COM3";
    public float speed = 4f;
    [Range(0f, 0.95f)] public float deadZone = 0.15f;
    public Vector2 center = new Vector2(512, 512);
    public bool invertX;
    public bool invertY;
    public bool swapAxes;
    [Header("Testing controls")]
    [Tooltip("Turn off before Play when testing without an Arduino.")]
    public bool useArduino = true;
    public bool mouseMovement = true;
    [Header("Shooting")]
    public CardinalGun gun;
    [SerializeField] private Camera movementCamera;

    [SerializeField] private PlayerInput playerInput;
    private Rigidbody2D body;
    private Vector2 movementDirection;
    private DesktopMovementInput desktopInput;
    private string activeInput = "Idle";
    private Rect OverlayRect => new Rect(12, 12, Mathf.Min(700, Screen.width - 24), 215);

    private readonly object gate = new object();
    private int rawX = 512, rawY = 512;
    private bool arduinoFire;
    private long lastSample;
    private string status = "Disconnected";
    private Texture2D squareTexture;
    private Sprite squareSprite;
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
    private Thread reader;
    private volatile bool stopping;
#endif


public bool isPlayable = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (gun == null) gun = GetComponentInChildren<CardinalGun>();
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
        if (renderer.sprite != null) return;
        squareTexture = new Texture2D(1, 1);
        squareTexture.SetPixel(0, 0, Color.white);
        squareTexture.Apply();
        squareSprite = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        renderer.sprite = squareSprite;
        renderer.color = new Color(0.2f, 0.9f, 0.65f);
    }

    private void OnEnable()
    {
        try
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
                throw new InvalidOperationException("Add a PlayerInput component to the player prefab.");
            if (desktopInput == null) desktopInput = new DesktopMovementInput(playerInput);
            desktopInput.Enable();
        }
        catch (Exception exception)
        {
            Debug.LogError("Player input could not start. Check the Player actions in InputSystem_Actions: "
                + exception.Message + " Fix the asset, then stop and restart Play.", this);
            enabled = false;
            return;
        }
        if (!useArduino)
        {
            lock (gate) { lastSample = 0; status = "Disabled (keyboard, mouse and gamepad available)"; }
            return;
        }
        lock (gate) { lastSample = 0; status = "Opening " + portName + "..."; }
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
        if (reader != null && reader.IsAlive)
        {
            lock (gate) status = "Previous connection is still closing. Stop and try Play again.";
            return;
        }
        stopping = false;
        string selectedPort = portName;
        reader = new Thread(() => ReadSerial(selectedPort)) { IsBackground = true };
        reader.Start();
#else
        status = "Run Double Dungeon > Set Up Joystick Demo to enable .NET Framework.";
#endif
    }

#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
    private void ReadSerial(string selectedPort)
    {
        try
        {
            using (var port = new SerialPort(selectedPort, 115200) { ReadTimeout = 100, DtrEnable = true, NewLine = "\n" })
            {
                port.Open();
                lock (gate) status = "Connected to " + selectedPort + "; waiting for joystick...";
                // Read incrementally so partial lines survive timeouts; bound malformed input.
                string pending = "";
                while (!stopping)
                {
                    int value;
                    try { value = port.ReadChar(); }
                    catch (TimeoutException) { continue; }
                    if (value < 0) continue;
                    if (value != '\n')
                    {
                        pending += (char)value;
                        if (pending.Length > 64) pending = "";
                        continue;
                    }
                    if (TryParseSample(pending, out int x, out int y, out bool fire))
                    {
                        lock (gate)
                        {
                            rawX = x; rawY = y;
                            arduinoFire = fire;
                            lastSample = System.Diagnostics.Stopwatch.GetTimestamp();
                            status = "Receiving on " + selectedPort;
                        }
                    }
                    pending = "";
                }
            }
        }
        catch (Exception exception)
        {
            lock (gate) { lastSample = 0; status = selectedPort + ": " + exception.Message; }
        }
    }
#endif

    public static bool TryParseSample(string line, out int x, out int y)
    {
        return TryParseSample(line, out x, out y, out _);
    }

    public static bool TryParseSample(string line, out int x, out int y, out bool fire)
    {
        x = y = 0;
        fire = false;
        if (string.IsNullOrEmpty(line)) return false;
        string[] parts = line.Split(',');
        if ((parts.Length != 2 && parts.Length != 3) || !int.TryParse(parts[0], out x)
            || !int.TryParse(parts[1], out y) || x < 0 || x > 1023 || y < 0 || y > 1023) return false;
        if (parts.Length == 2) return true; // Older movement-only sketch.
        if (!int.TryParse(parts[2], out int button) || (button != 0 && button != 1)) return false;
        fire = button == 1;
        return true;
    }

    private bool IsFresh(long stamp)
    {
        return stamp != 0 && (System.Diagnostics.Stopwatch.GetTimestamp() - stamp)
            / (double)System.Diagnostics.Stopwatch.Frequency < 0.5;
    }

    private float Axis(int value, float midpoint)
    {
        float normalized = (value - midpoint) / Mathf.Max(1, value >= midpoint ? 1023 - midpoint : midpoint);
        float magnitude = Mathf.Abs(normalized);
        return magnitude <= deadZone ? 0 : Mathf.Sign(normalized) * Mathf.Clamp01((magnitude - deadZone) / (1 - deadZone));
    }

    private void Update()
    {
        movementDirection = Vector2.zero;
        if (!isPlayable || desktopInput == null || !Application.isFocused)
        {
            activeInput = "Idle";
            return;
        }
        int x, y; long stamp; bool serialFire;
        lock (gate) { x = rawX; y = rawY; stamp = lastSample; serialFire = arduinoFire; }
        float step = Mathf.Max(0, speed) * Time.deltaTime;
        if (!desktopInput.TryGetMovement(transform.position, movementCamera, step, OverlayRect, mouseMovement,
            out Vector2 direction, out activeInput))
        {
            direction = Vector2.zero;
            if (useArduino && IsFresh(stamp))
            {
                direction = new Vector2(Axis(x, center.x), Axis(y, center.y));
                if (swapAxes) direction = new Vector2(direction.y, direction.x);
                if (invertX) direction.x *= -1;
                if (invertY) direction.y *= -1;
                if (direction.sqrMagnitude > 0) activeInput = "Arduino";
            }
        }
        movementDirection = Vector2.ClampMagnitude(direction, 1);
        if (gun != null)
            gun.Tick(direction, desktopInput.FireHeld || (useArduino && IsFresh(stamp) && serialFire));
    }

    private void FixedUpdate()
    {
        if (!isPlayable)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = movementDirection * speed;
        Camera camera = movementCamera;
        if (camera != null && camera.orthographic)
        {
            float depth = Mathf.Abs(transform.position.z - camera.transform.position.z);
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            Collider2D collider = GetComponent<Collider2D>();
            Vector2 extents = collider != null ? collider.bounds.extents : Vector2.zero;
            Vector3 position = body.position;
            position.x = Mathf.Clamp(position.x, bottomLeft.x + extents.x, topRight.x - extents.x);
            position.y = Mathf.Clamp(position.y, bottomLeft.y + extents.y, topRight.y - extents.y);
            body.position = position;
        }
    }

    public void SetMovementCamera(Camera camera)
    {
        movementCamera = camera;
    }

    // private void OnGUI()
    // {
    //     int x, y; long stamp; string message;
    //     lock (gate) { x = rawX; y = rawY; stamp = lastSample; message = status; }
    //     GUILayout.BeginArea(OverlayRect, GUI.skin.box);
    //     GUILayout.Label("MOVEMENT | " + activeInput);
    //     GUILayout.Label("WASD / arrows | Gamepad left stick / D-pad | Hold right mouse to move");
    //     GUILayout.Label("Hold fire: left mouse / Space / gamepad right trigger or west button");
    //     GUILayout.Label("Arduino: " + message);
    //     GUILayout.Label("Raw X: " + x + "   Raw Y: " + y + (IsFresh(stamp) ? "" : "   (no recent data)"));
    //     GUI.enabled = useArduino && IsFresh(stamp);
    //     if (GUILayout.Button("Calibrate center (release joystick first)")) center = new Vector2(x, y);
    //     GUI.enabled = true;
    //     GUILayout.Label("Set Port Name on Player in the Inspector. Stop/Play to reconnect.");
    //     GUILayout.EndArea();
    // }

    private void OnDisable()
    {
        desktopInput?.Disable();
        activeInput = "Idle";
#if !NET_STANDARD_2_0 && !NET_STANDARD_2_1
        stopping = true;
        if (reader != null && !reader.Join(1500))
            Debug.LogWarning("Serial reader is still closing. Wait before reopening the port.");
        if (reader != null && !reader.IsAlive) reader = null;
#endif
        lock (gate) lastSample = 0;
    }

    private void OnDestroy()
    {
        desktopInput?.Dispose();
        if (squareSprite != null) Destroy(squareSprite);
        if (squareTexture != null) Destroy(squareTexture);
    }
}

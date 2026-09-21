using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

// Keep one manager in the gameplay scene. Slot assignments are explicit, not COM-port order.
[DisallowMultipleComponent]
public class ardunoManager : MonoBehaviour
{
    [Serializable]
    public class ArduinoSlot
    {
        public bool enabled;
        public string portName = "COM3";
        public PlayerInput player;
        public Vector2 center = new Vector2(512, 512);
        [Range(0, 0.95f)] public float deadZone = 0.15f;
        public bool invertX, invertY, swapAxes;
        [NonSerialized] public string status = "Disabled";
        [NonSerialized] internal ArduinoSerialConnection connection;
        [NonSerialized] internal ArduinoInputDevice device;
        [NonSerialized] internal PlayerInput pairedPlayer;
        [NonSerialized] internal InputDevice[] previousDevices;
        [NonSerialized] internal string previousScheme;
        [NonSerialized] internal bool previousAutoSwitch;
        [NonSerialized] internal string attemptedPort;
        [NonSerialized] internal string pairingError;
        [NonSerialized] internal bool fireWasHeld;
        [NonSerialized] internal bool suppressFireUntilRelease;
        [NonSerialized] internal string joinStatus;
    }

    public ArduinoSlot[] slots = { new ArduinoSlot(), new ArduinoSlot(), new ArduinoSlot(), new ArduinoSlot() };
    [Header("Joining")]
    public bool pressFireToJoin = true;
    [Tooltip("Uses the active PlayerInputManager automatically when left empty.")]
    public PlayerInputManager playerInputManager;
    [Min(0.1f)] public float staleAfterSeconds = 0.5f;
    [Tooltip("Release Arduino actions while the application is unfocused. Turn off only for background input testing.")]
    public bool releaseWhenUnfocused = true;
    private static ardunoManager activeManager;
    private readonly HashSet<ArduinoSlot> trackedSlots = new HashSet<ArduinoSlot>();

    private void OnEnable()
    {
        if (activeManager != null && activeManager != this)
        {
            Debug.LogError("Only one ardunoManager may be enabled at a time.", this);
            enabled = false;
            return;
        }
        activeManager = this;
        ArduinoInputDevice.Register();
        InputSystem.onBeforeUpdate += PublishInputs;
        if (slots != null && slots.Length > 4) Debug.LogWarning("Only the first four Arduino slots are used.", this);
    }

    private void Update()
    {
        foreach (var tracked in trackedSlots)
        {
            int index = slots == null ? -1 : Array.IndexOf(slots, tracked);
            if (index < 0 || index >= 4) StopSlot(tracked);
        }
        var ports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var players = new HashSet<PlayerInput>();
        for (int i = 0; slots != null && i < Mathf.Min(4, slots.Length); i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            trackedSlots.Add(slot);
            string port = (slot.portName ?? "").Trim();
            string problem = !slot.enabled ? "Disabled" : string.IsNullOrEmpty(port) ? "Enter a COM port" : null;
            if (problem == null && !ports.Add(port)) problem = "Duplicate COM port";
            if (problem == null && slot.player != null && !players.Add(slot.player)) problem = "Player already assigned to another slot";
            if (problem != null)
            {
                StopSlot(slot);
                slot.status = problem;
                continue;
            }
            if (slot.attemptedPort != port)
            {
                StopSlot(slot);
                if (slot.connection != null) { slot.status = "Waiting for previous port to close"; continue; }
                slot.attemptedPort = port;
                slot.connection = new ArduinoSerialConnection(port);
                slot.connection.Start();
            }
            if (slot.device != null && (slot.player == null || slot.pairedPlayer != slot.player || !slot.player.isActiveAndEnabled))
                Unpair(slot);
            if (slot.player != null && slot.player.isActiveAndEnabled && slot.device == null && slot.pairingError == null)
                Pair(slot, i);
            var sample = slot.connection?.ReadSnapshot() ?? default;
            bool fresh = ArduinoSerialConnection.IsFresh(sample.receivedAt, staleAfterSeconds);
            bool fireHeld = fresh && sample.fire && (!releaseWhenUnfocused || Application.isFocused);
            if (fresh && !sample.fire) slot.suppressFireUntilRelease = false;
            if (slot.player == null && pressFireToJoin && fireHeld && !slot.fireWasHeld)
            {
                TryJoin(slot, i);
                if (slot.player != null) players.Add(slot.player);
            }
            slot.fireWasHeld = fireHeld;
            slot.status = sample.status ?? "Not connected";
            if (sample.receivedAt != 0 && !ArduinoSerialConnection.IsFresh(sample.receivedAt, staleAfterSeconds))
                slot.status = "No recent data — movement and fire released";
            if (slot.pairingError != null) slot.status += " | " + slot.pairingError;
            else if (slot.device == null)
                slot.status += " | " + (slot.joinStatus ?? (pressFireToJoin ? "Press fire to join" : "Assign an active scene PlayerInput"));
        }
    }

    private void TryJoin(ArduinoSlot slot, int index)
    {
        var joining = playerInputManager != null ? playerInputManager : PlayerInputManager.instance;
        if (joining == null || !joining.isActiveAndEnabled)
        {
            slot.joinStatus = "Scene needs an active PlayerInputManager with a Player Prefab";
            return;
        }
        if (!joining.joiningEnabled) { slot.joinStatus = "Joining is currently disabled"; return; }
        if (joining.playerCount >= 4 || (joining.maxPlayerCount >= 0 && joining.playerCount >= joining.maxPlayerCount))
        {
            slot.joinStatus = "Player limit reached";
            return;
        }
        var prefabInput = joining.playerPrefab != null ? joining.playerPrefab.GetComponentInChildren<PlayerInput>(true) : null;
        if (prefabInput == null || prefabInput.actions == null || !prefabInput.actions.FindControlScheme("Arduino").HasValue)
        {
            slot.joinStatus = "Player Prefab needs PlayerInput actions with the Arduino scheme";
            return;
        }
        try
        {
            slot.device = InputSystem.AddDevice<ArduinoInputDevice>("Arduino " + (index + 1));
            // This uses the existing join notification, so DungeonManager can make the player playable.
            var joined = joining.JoinPlayer(controlScheme: "Arduino", pairWithDevice: slot.device);
            if (joined == null)
            {
                Unpair(slot);
                slot.joinStatus = "Could not join; release fire and try again";
                return;
            }
            slot.player = joined;
            slot.pairedPlayer = joined;
            slot.previousAutoSwitch = joined.neverAutoSwitchControlSchemes;
            slot.previousDevices = Array.Empty<InputDevice>();
            slot.previousScheme = null;
            joined.neverAutoSwitchControlSchemes = true;
            slot.suppressFireUntilRelease = true;
            slot.joinStatus = null;
        }
        catch (Exception exception)
        {
            Unpair(slot);
            slot.joinStatus = "Join failed: " + exception.Message;
        }
    }

    private void Pair(ArduinoSlot slot, int index)
    {
        try
        {
            if (slot.player.actions == null || !slot.player.actions.FindControlScheme("Arduino").HasValue)
                throw new InvalidOperationException("PlayerInput actions need the Arduino control scheme.");
            slot.previousDevices = slot.player.devices.ToArray();
            slot.previousScheme = slot.player.currentControlScheme;
            slot.previousAutoSwitch = slot.player.neverAutoSwitchControlSchemes;
            slot.device = InputSystem.AddDevice<ArduinoInputDevice>("Arduino " + (index + 1));
            // Remove automatic claims made by other PlayerInputs when the device was added.
            foreach (var user in InputUser.all)
                foreach (var paired in user.pairedDevices)
                    if (paired == slot.device) { user.UnpairDevice(slot.device); break; }
            slot.pairedPlayer = slot.player;
            slot.player.neverAutoSwitchControlSchemes = true;
            // A PlayerInput enabled before any matching device exists has no InputUser yet.
            // Re-enable just that component now that its device exists, using the Arduino scheme.
            if (!slot.player.user.valid)
            {
                string previousDefault = slot.player.defaultControlScheme;
                bool wasActive = slot.player.inputIsActive;
                try
                {
                    slot.player.defaultControlScheme = "Arduino";
                    slot.player.enabled = false;
                    slot.player.enabled = true;
                    if (!wasActive) slot.player.DeactivateInput();
                }
                finally { slot.player.defaultControlScheme = previousDefault; }
            }
            slot.player.SwitchCurrentControlScheme("Arduino", slot.device);
        }
        catch (Exception exception)
        {
            Unpair(slot);
            slot.pairingError = exception.Message;
        }
    }

    private void PublishInputs()
    {
        for (int i = 0; slots != null && i < Mathf.Min(4, slots.Length); i++)
        {
            var slot = slots[i];
            if (slot?.device == null || !slot.device.added) continue;
            var state = new ArduinoInputState();
            var sample = slot.connection?.ReadSnapshot() ?? default;
            if (slot.enabled && slot.player == slot.pairedPlayer && slot.player != null
                && slot.player.isActiveAndEnabled && (!releaseWhenUnfocused || Application.isFocused)
                && ArduinoSerialConnection.IsFresh(sample.receivedAt, staleAfterSeconds))
            {
                state.move = Calibrate(sample.x, sample.y, slot);
                state.fire = sample.fire && !slot.suppressFireUntilRelease ? 1 : 0;
            }
            InputSystem.QueueStateEvent(slot.device, state);
        }
    }

    public static Vector2 Calibrate(int x, int y, ArduinoSlot slot)
    {
        Vector2 value = new Vector2(Axis(x, slot.center.x, slot.deadZone), Axis(y, slot.center.y, slot.deadZone));
        if (slot.swapAxes) value = new Vector2(value.y, value.x);
        if (slot.invertX) value.x = -value.x;
        if (slot.invertY) value.y = -value.y;
        return Vector2.ClampMagnitude(value, 1);
    }

    private static float Axis(int raw, float center, float deadZone)
    {
        center = Mathf.Clamp(center, 1, 1022);
        deadZone = Mathf.Clamp(deadZone, 0, 0.95f);
        float value = (raw - center) / (raw >= center ? 1023 - center : center);
        return Mathf.Sign(value) * Mathf.Clamp01((Mathf.Abs(value) - deadZone) / (1 - deadZone));
    }

    // For a future spawning/lobby system. Index is 0..3; assignment is checked next Update.
    public void AssignPlayer(int slotIndex, PlayerInput player)
    {
        if (slotIndex < 0 || slotIndex >= 4 || slots == null || slotIndex >= slots.Length)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
        if (slots[slotIndex] == null) slots[slotIndex] = new ArduinoSlot();
        Unpair(slots[slotIndex]);
        slots[slotIndex].player = player;
    }

    public bool CalibrateCenter(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= Mathf.Min(4, slots.Length)) return false;
        var slot = slots[slotIndex];
        var sample = slot?.connection?.ReadSnapshot() ?? default;
        if (!ArduinoSerialConnection.IsFresh(sample.receivedAt, staleAfterSeconds)) return false;
        slot.center = new Vector2(sample.x, sample.y);
        return true;
    }

    [ContextMenu("Reconnect All Arduinos")]
    public void ReconnectAll()
    {
        foreach (var slot in trackedSlots) StopSlot(slot);
        if (slots != null)
            foreach (var slot in slots) if (slot != null && !trackedSlots.Contains(slot)) StopSlot(slot);
    }

    private void StopSlot(ArduinoSlot slot)
    {
        Unpair(slot);
        slot.connection?.Stop();
        if (slot.connection != null && slot.connection.HasStopped) slot.connection = null;
        slot.attemptedPort = null;
        slot.fireWasHeld = false;
        slot.joinStatus = null;
    }

    private void Unpair(ArduinoSlot slot)
    {
        if (slot.device != null && slot.device.added) InputSystem.RemoveDevice(slot.device);
        slot.device = null;
        if (slot.pairedPlayer != null)
        {
            slot.pairedPlayer.neverAutoSwitchControlSchemes = slot.previousAutoSwitch;
            if (slot.pairedPlayer.isActiveAndEnabled && !string.IsNullOrEmpty(slot.previousScheme)
                && slot.previousDevices != null && Array.TrueForAll(slot.previousDevices, device => device != null && device.added))
            {
                try { slot.pairedPlayer.SwitchCurrentControlScheme(slot.previousScheme, slot.previousDevices); }
                catch (Exception) { /* Original device combination may no longer be available. */ }
            }
        }
        slot.pairedPlayer = null;
        slot.pairingError = null;
    }

    private void OnDisable()
    {
        InputSystem.onBeforeUpdate -= PublishInputs;
        ReconnectAll();
        if (activeManager == this) activeManager = null;
    }
}

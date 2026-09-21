using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] private bool debugMode = false;
    [SerializeField] private PlayerInputManager playerInputManager;
    // Spawn points are indexed by PlayerInput.playerIndex.
    [SerializeField] private Transform[] playerSpawnPoints;
    // Cameras define the movement bounds. Multiple players may share one camera.
    [SerializeField] private Camera[] movementCameras;
    // Each entry should be the target group followed by the matching Cinemachine camera.
    [SerializeField] private CinemachineTargetGroup[] cameraTargetGroups;
    // Optional per-player camera mapping. If empty, GetCameraIndex uses playerIndex / 2.
    [SerializeField] private int[] playerCameraIndices;
    private List<ArduinoJoystickPlayer> players = new List<ArduinoJoystickPlayer>();
    private bool wasDebugModeEnabled;
    private int knownPlayerCount;

    public event System.Action DebugModeEnabled;

    // Initializes players that were carried over from the menu scene.
    void Start()
    {
        wasDebugModeEnabled = debugMode;
        knownPlayerCount = playerInputManager != null ? playerInputManager.playerCount : 0;
        players.AddRange(FindObjectsByType<ArduinoJoystickPlayer>());
        if (players.Count > 0)
        {
            foreach (ArduinoJoystickPlayer player in players)
            {
                // Players persist across the scene change, so place and configure them here.
                MovePlayerToSpawnPoint(player);
                AssignPlayerCamera(player);
                SetPlayerPlayable(player);
            }
            Debug.Log(FindObjectsByType<ArduinoJoystickPlayer>().Length + " ArduinoJoystickPlayers found and set to playable.");
        }
        else
        {
            playerInputManager.EnableJoining();
            Debug.Log("No ArduinoJoystickPlayers found. Players can join by pressing the Join button.");
        }

        if (debugMode)
            EnableDebugMode();
    }

    private void OnEnable()
    {
        // PlayerInputManager must notify this script when a player joins during gameplay.
        if (playerInputManager == null)
        {
            Debug.LogError("DungeonManager needs a PlayerInputManager reference.", this);
            return;
        }

        playerInputManager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid callbacks after this manager is disabled or destroyed.
        if (playerInputManager == null) return;
        playerInputManager.onPlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerInput playerInput)
    {
        // A PlayerInput can be on the root while the movement component is on a child.
        ArduinoJoystickPlayer player = playerInput.transform.root
            .GetComponentInChildren<ArduinoJoystickPlayer>(true);

        if (player == null)
        {
            Debug.LogWarning("Joined player " + playerInput.playerIndex
                + " has no ArduinoJoystickPlayer component.", playerInput);
            return;
        }

        if (!players.Contains(player)) players.Add(player);
        MovePlayerToSpawnPoint(player);
        AssignPlayerCamera(player);
        knownPlayerCount = playerInputManager.playerCount;
        SetPlayerPlayable(player);
        Debug.Log("Player " + playerInput.playerIndex + " joined and is now playable.", player);
    }

    private void MovePlayerToSpawnPoint(ArduinoJoystickPlayer player)
    {
        PlayerInput playerInput = player.GetComponentInParent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("Player has no parent PlayerInput component.", player);
            return;
        }

        // Player indices provide a stable mapping from a joined player to its spawn.
        int spawnIndex = playerInput.playerIndex;
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
        {
            Debug.LogWarning("DungeonManager has no player spawn points assigned.", this);
            return;
        }

        if (spawnIndex < 0 || spawnIndex >= playerSpawnPoints.Length)
        {
            Debug.LogWarning("No spawn point is assigned for player " + spawnIndex + ".", this);
            return;
        }

        Transform spawnPoint = playerSpawnPoints[spawnIndex];
        if (spawnPoint == null)
        {
            Debug.LogWarning("The spawn point for player " + spawnIndex + " is not assigned.", this);
            return;
        }

        playerInput.transform.root.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation);
    }

    private void AssignPlayerCamera(ArduinoJoystickPlayer player)
    {
        PlayerInput playerInput = player.GetComponentInParent<PlayerInput>();
        if (playerInput == null) return;

        // Several players can use the same camera and therefore share its movement area.
        int playerIndex = playerInput.playerIndex;
        int cameraIndex = GetCameraIndex(playerIndex);
        if (movementCameras == null || cameraIndex < 0 || cameraIndex >= movementCameras.Length)
        {
            Debug.LogWarning("No movement camera is assigned for player " + playerIndex + ".", this);
            return;
        }

        Camera movementCamera = movementCameras[cameraIndex];
        if (movementCamera == null)
        {
            Debug.LogWarning("Movement camera " + cameraIndex + " is not assigned.", this);
            return;
        }

        player.SetMovementCamera(movementCamera);
        AssignPlayerTargetGroup(player, cameraIndex);
    }

    private void AssignPlayerTargetGroup(ArduinoJoystickPlayer player, int cameraIndex)
    {
        if (cameraTargetGroups == null || cameraIndex < 0
            || cameraIndex >= cameraTargetGroups.Length)
            return;

        CinemachineTargetGroup targetGroup = cameraTargetGroups[cameraIndex];
        if (targetGroup == null) return;

        Transform target = player.transform.root;
        foreach (CinemachineTargetGroup.Target groupTarget in targetGroup.Targets)
        {
            if (groupTarget.Object == target) return;
        }

        targetGroup.Targets.Add(new CinemachineTargetGroup.Target
        {
            Object = target,
            Weight = 1f,
            Radius = 0.5f
        });
    }

    private int GetCameraIndex(int playerIndex)
    {
        // Use an explicit mapping when supplied; otherwise group players in pairs:
        // players 0 and 1 use camera 0, players 2 and 3 use camera 1, and so on.
        if (playerCameraIndices != null && playerIndex >= 0
            && playerIndex < playerCameraIndices.Length)
            return playerCameraIndices[playerIndex];

        return playerIndex / 2;
    }

    private static void SetPlayerPlayable(ArduinoJoystickPlayer player)
    {
        player.isPlayable = true;
    }

    // In debug mode, detect players added after the initial scene setup.
    void Update()
    {
        if (debugMode && !wasDebugModeEnabled)
            EnableDebugMode();

        wasDebugModeEnabled = debugMode;

        if (debugMode && playerInputManager != null
            && playerInputManager.playerCount != knownPlayerCount)
        {
            knownPlayerCount = playerInputManager.playerCount;
            foreach (ArduinoJoystickPlayer player in FindObjectsByType<ArduinoJoystickPlayer>())
            {
                if (!players.Contains(player)) players.Add(player);
                SetPlayerPlayable(player);
            }
        }
    }

    private void EnableDebugMode()
    {
        // Debug mode allows additional players to join after the dungeon starts.
        wasDebugModeEnabled = true;
        playerInputManager.EnableJoining();
        DebugModeEnabled?.Invoke();
        Debug.Log("Debug mode enabled. Additional players can join.", this);
    }
}

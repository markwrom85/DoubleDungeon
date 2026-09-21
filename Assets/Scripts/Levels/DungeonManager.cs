using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] private bool debugMode = false;
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Camera[] movementCameras;
    [SerializeField] private int[] playerCameraIndices;
    private List<ArduinoJoystickPlayer> players = new List<ArduinoJoystickPlayer>();
    private bool wasDebugModeEnabled;
    private int knownPlayerCount;

    public event System.Action DebugModeEnabled;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wasDebugModeEnabled = debugMode;
        knownPlayerCount = playerInputManager != null ? playerInputManager.playerCount : 0;
        players.AddRange(FindObjectsByType<ArduinoJoystickPlayer>());
        if (players.Count > 0)
        {
            foreach (ArduinoJoystickPlayer player in players)
            {
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
        if (playerInputManager == null) return;
        playerInputManager.onPlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerInput playerInput)
    {
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
    }

    private int GetCameraIndex(int playerIndex)
    {
        if (playerCameraIndices != null && playerIndex >= 0
            && playerIndex < playerCameraIndices.Length)
            return playerCameraIndices[playerIndex];

        return playerIndex / 2;
    }

    private static void SetPlayerPlayable(ArduinoJoystickPlayer player)
    {
        player.isPlayable = true;
    }

    // Update is called once per frame
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
        wasDebugModeEnabled = true;
        playerInputManager.EnableJoining();
        DebugModeEnabled?.Invoke();
        Debug.Log("Debug mode enabled. Additional players can join.", this);
    }
}

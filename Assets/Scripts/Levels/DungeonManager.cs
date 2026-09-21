using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonManager : MonoBehaviour
{
    public bool debugMode = false;
    [SerializeField] private PlayerInputManager playerInputManager;
    private List<ArduinoJoystickPlayer> players = new List<ArduinoJoystickPlayer>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        players.AddRange(FindObjectsByType<ArduinoJoystickPlayer>());
        if (players.Count > 0)
        {
            foreach (ArduinoJoystickPlayer player in players)
            {
                SetPlayerPlayable(player);
            }
            Debug.Log(FindObjectsByType<ArduinoJoystickPlayer>().Length + " ArduinoJoystickPlayers found and set to playable.");
        }
        else
        {
            playerInputManager.EnableJoining();
            Debug.Log("No ArduinoJoystickPlayers found. Players can join by pressing the Join button.");
        }
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
        ArduinoJoystickPlayer player = playerInput.GetComponent<ArduinoJoystickPlayer>();
        if (player == null)
            player = playerInput.GetComponentInChildren<ArduinoJoystickPlayer>();

        if (player == null)
        {
            Debug.LogWarning("Joined player " + playerInput.playerIndex
                + " has no ArduinoJoystickPlayer component.", playerInput);
            return;
        }

        if (!players.Contains(player)) players.Add(player);
        SetPlayerPlayable(player);
    }

    private static void SetPlayerPlayable(ArduinoJoystickPlayer player)
    {
        player.isPlayable = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}

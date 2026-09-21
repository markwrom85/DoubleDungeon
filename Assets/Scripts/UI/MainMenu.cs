using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startingMenu;
    [SerializeField] private GameObject lobbyMenu;
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private InputAction joinAction;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private GameManager gameManager;
    private readonly System.Collections.Generic.List<PlayerInput> joinedPlayers =
        new System.Collections.Generic.List<PlayerInput>();
    private bool canJoin = false;
    private GameObject currentMenu;

    private void Start()
    {
        if (startingMenu != null)
        {
            currentMenu = startingMenu;
            currentMenu.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Starting menu is not assigned in the inspector.");
        }

        SetJoining(currentMenu == lobbyMenu);
    }

    private void OnEnable()
    {
        playerInputManager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        joinAction.performed += OnJoinActionPerformed;
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        joinAction.performed -= OnJoinActionPerformed;
        playerInputManager.onPlayerJoined -= OnPlayerJoined;
        joinAction.Disable();
        playerInputManager.DisableJoining();
    }

    public void ChangeMenu(GameObject menuToActivate)
    {
        currentMenu.SetActive(false);
        menuToActivate.SetActive(true);
        currentMenu = menuToActivate;
        SetJoining(currentMenu == lobbyMenu);
    }

    public void AddPlayer()
    {
        if (canJoin)
            playerInputManager.JoinPlayer();
    }

    private void OnJoinActionPerformed(InputAction.CallbackContext context)
    {
        if (!canJoin)
            return;

        playerInputManager.JoinPlayerFromActionIfNotAlreadyJoined(context);
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        if (!joinedPlayers.Contains(player)) joinedPlayers.Add(player);

        int spawnIndex = player.playerIndex;
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

        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    private void SetJoining(bool enabled)
    {
        canJoin = enabled;

        if (enabled)
        {
            playerInputManager.EnableJoining();
            joinAction.Enable();
        }
        else
        {
            playerInputManager.DisableJoining();
            joinAction.Disable();
            foreach (PlayerInput player in joinedPlayers)
            {
                if (player != null)
                    Destroy(player.gameObject);
            }
            joinedPlayers.Clear();
        }
    }

    public void StartGame()
    {
        if (joinedPlayers.Count == 0)
        {
            Debug.LogWarning("Join at least one player before starting the game.", this);
            return;
        }

        GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
        if (manager == null)
        {
            Debug.LogError("A GameManager is required to start the gameplay scene.", this);
            return;
        }

        foreach (PlayerInput player in joinedPlayers)
            manager.RegisterPlayer(player);

        manager.LoadGameplayScene(gameplaySceneName);
    }
}

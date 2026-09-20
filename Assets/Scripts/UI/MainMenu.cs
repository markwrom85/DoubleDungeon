using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startingMenu;
    [SerializeField] private GameObject lobbyMenu;
    [SerializeField] private PlayerInputManager playerInputManager;
    [SerializeField] private InputAction joinAction;
    [SerializeField] private Transform[] playerSpawnPoints;
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
        }
    }

    public void StartGame()
    {

    }
}

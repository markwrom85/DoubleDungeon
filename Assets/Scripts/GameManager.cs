using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly List<PlayerInput> players = new List<PlayerInput>();
    private bool movePlayersToLoadedScene;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    public void RegisterPlayer(PlayerInput player)
    {
        if (player == null || players.Contains(player)) return;

        players.Add(player);
        DontDestroyOnLoad(player.transform.root.gameObject);
        Debug.Log("Registered player " + player.playerIndex + " using "
            + (player.currentControlScheme ?? "unassigned") + " ("
            + string.Join(", ", GetDeviceNames(player)) + ").", this);
    }

    public void LoadGameplayScene(string sceneName)
    {
        movePlayersToLoadedScene = true;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!movePlayersToLoadedScene) return;

        movePlayersToLoadedScene = false;
        foreach (PlayerInput player in players)
        {
            if (player == null) continue;
            SceneManager.MoveGameObjectToScene(player.transform.root.gameObject, scene);
        }
    }

    private static string[] GetDeviceNames(PlayerInput player)
    {
        var names = new string[player.devices.Count];
        for (int index = 0; index < player.devices.Count; index++)
            names[index] = player.devices[index].displayName;
        return names;
    }
}

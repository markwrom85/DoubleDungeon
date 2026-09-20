using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startingMenu;
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
    }

    public void ChangeMenu(GameObject menuToActivate)
    {
        currentMenu.SetActive(false);
        menuToActivate.SetActive(true);
        currentMenu = menuToActivate;
    }

    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}

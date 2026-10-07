using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private Door[] doors;

    private void Start()
    {
        if (combatManager == null) return;
        combatManager.AllEnemiesDefeated += OnAllEnemiesDefeated;
        if (combatManager.AreAllEnemiesDefeated)
            OnAllEnemiesDefeated();

        combatManager = FindAnyObjectByType<CombatManager>();
    }

    private void OnDestroy()
    {
        if (combatManager != null)
            combatManager.AllEnemiesDefeated -= OnAllEnemiesDefeated;
    }

    private void OnAllEnemiesDefeated()
    {
        foreach(Door door in doors)
        {
            door.isOpen = true;
            door.GetComponentInChildren<SpriteRenderer>().color = Color.green;
        }
    }
}
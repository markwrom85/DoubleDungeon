using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private Door[] doors;

    private void Start()
    {
        if (combatManager == null)
            combatManager = FindAnyObjectByType<CombatManager>();
        if (combatManager == null) return;
        combatManager.AllEnemiesDefeated += OnAllEnemiesDefeated;
        if (combatManager.AreAllEnemiesDefeated)
            OnAllEnemiesDefeated();

        combatManager.CombatStarted += OnCombatStarted;
        if (combatManager.IsInCombat)
            OnCombatStarted();
    }

    private void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.AllEnemiesDefeated -= OnAllEnemiesDefeated;
            combatManager.CombatStarted -= OnCombatStarted;
        }
    }

    private void OnAllEnemiesDefeated()
    {
        foreach(Door door in doors)
        {
            door.isOpen = true;
            door.GetComponentInChildren<SpriteRenderer>().color = Color.green;
        }
    }

    private void OnCombatStarted()
    {
        foreach (Door door in doors)
        {
            door.isOpen = false;
            door.GetComponentInChildren<SpriteRenderer>().color = Color.red;
        }
    }
}
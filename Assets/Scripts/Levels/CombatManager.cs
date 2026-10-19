using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public List<EnemyController> ActiveEnemies { get; } = new();
    public bool AreAllEnemiesDefeated => ActiveEnemies.Count == 0;
    public bool IsInCombat => inCombat;
    private bool inCombat = false;
    public event System.Action AllEnemiesDefeated;
    public event System.Action CombatStarted;

    public void RegisterEnemy(EnemyController enemy)
    {
        if (!ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Add(enemy);
            if(!inCombat)
            {
                inCombat = true;
                CombatStarted?.Invoke();
            }
        }
        // Debug.Log($"Registered enemy: {enemy.name}. Total active enemies: {ActiveEnemies.Count}");
    }
    public void OnEnemyDefeated(EnemyController enemy)
    {
        if (!ActiveEnemies.Remove(enemy)) return;
        // Debug.Log($"Enemy defeated: {enemy.name}. Remaining active enemies: {ActiveEnemies.Count}");
        if (AreAllEnemiesDefeated)
        {
            AllEnemiesDefeated?.Invoke();
            inCombat = false;
        }
    }
}
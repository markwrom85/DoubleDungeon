using UnityEngine;

public class ItemSpawn : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private ItemInfo[] spawnedItems;
    [SerializeField] private Transform spawnPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        combatManager = FindAnyObjectByType<CombatManager>();
        combatManager.AllEnemiesDefeated += SpawnItem;
    }

    private void SpawnItem()
    {
        Instantiate(spawnedItems[Random.Range(0, spawnedItems.Length)], spawnPosition);
    }
}

using UnityEngine;

public class ItemSpawn : MonoBehaviour
{
    [SerializeField] private Room room;
    [SerializeField] private ItemInfo[] spawnedItems;
    [SerializeField] private Transform spawnPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        room = GetComponent<Room>();
        room.RoomCompleted += SpawnItem;
    }

    private void SpawnItem()
    {
        if (room.roomActive)
        {
            Instantiate(spawnedItems[Random.Range(0, spawnedItems.Length)], spawnPosition);
            room.roomActive = false;
        }
    }
}

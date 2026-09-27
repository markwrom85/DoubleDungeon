using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private PlayerInfo playerInfo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInfo = GetComponent<PlayerInfo>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            ItemInfo item = other.GetComponent<ItemInfo>();
            if (item != null)
            {
                playerInfo.ChangeStats(item.MoveSpeed, item.MaxHealth, item.AttackDamage, item.AttackRate, item.SwapDuration);
                Destroy(other.gameObject);
            }
        }
    }
}

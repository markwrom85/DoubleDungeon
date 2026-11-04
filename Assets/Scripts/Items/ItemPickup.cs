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
        if (!other.CompareTag("Item"))
            return;

        ItemInfo item = other.GetComponent<ItemInfo>();
        if (item == null || !item.enabled)
            return;

        item.enabled = false;
        Collider2D itemCollider = other.GetComponent<Collider2D>();
        if (itemCollider != null)
            itemCollider.enabled = false;

        playerInfo.ChangeStats(
            item.MoveSpeed,
            item.MaxHealth,
            item.AttackDamage,
            item.AttackRate,
            item.AOEDamage,
            item.SwapCooldown,
            item.SwapDuration
        );

        if (item.ModifierPrefab != null)
        {
            if (item.ModifierPrefab.GetComponent<SpecialAttack>() != null)
            {
                if (playerInfo.spAttack != null)
                {
                    playerInfo.spAttack.gameObject.SetActive(false);
                    Destroy(playerInfo.spAttack.gameObject);
                }

                Instantiate(item.ModifierPrefab, playerInfo.transform, false);
            }
            else
            {
                GameObject modifier = Instantiate(
                    item.ModifierPrefab,
                    other.transform.position,
                    Quaternion.identity
                );
                modifier.transform.SetParent(playerInfo.transform, true);
            }
        }

        Transform pedestal = other.transform.Find("Pedestal");
        if (pedestal != null)
            pedestal.SetParent(other.transform.parent, true);

        Destroy(other.gameObject);
    }
}

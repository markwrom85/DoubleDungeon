using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    [SerializeField] private Item itemInfo;
    [SerializeField] private GameObject modifierPrefab;
    [SerializeField] private TomeDefinition tome;
    [SerializeField] private Transform pedestal;
    private bool collected;

    public bool TryCollect(PlayerInfo player)
    {
        if (collected || player == null) return false;
        if (tome != null && !player.EquipTome(tome)) return false;
        collected = true;
        player.ChangeStats(MoveSpeed, MaxHealth, AttackDamage, AttackRate, AOEDamage, SwapCooldown, SwapDuration);
        if (tome == null) AddGameplayModifier(player.transform);
        if (pedestal != null) pedestal.SetParent(transform.parent, true);
        Destroy(gameObject);
        return true;
    }

    public float MoveSpeed => itemInfo.moveSpeed;
    public int MaxHealth => itemInfo.maxHealth;
    public float AttackDamage => itemInfo.attackDamage;
    public float AttackRate => itemInfo.attackRate;
    public float AOEDamage => itemInfo.aoeDamage;
    public float SwapCooldown => itemInfo.swapCooldown;
    public float SwapDuration => itemInfo.swapDuration;

    public void AddGameplayModifier(Transform playerTransform)
    {
        if(modifierPrefab != null)
        {
            GameObject modifier = Instantiate(modifierPrefab, transform.position, Quaternion.identity);
            modifier.transform.SetParent(playerTransform);
        }
    }
}

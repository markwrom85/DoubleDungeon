using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    [SerializeField] private Item itemInfo;
    [SerializeField] private GameObject modifierPrefab;

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

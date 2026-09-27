using UnityEngine;

public class ItemInfo : MonoBehaviour
{
    [SerializeField] private Item itemInfo;

    public float MoveSpeed => itemInfo.moveSpeed;
    public int MaxHealth => itemInfo.maxHealth;
    public float AttackDamage => itemInfo.attackDamage;
    public float AttackRate => itemInfo.attackRate;
    public float SwapDuration => itemInfo.swapDuration;
}

using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public float moveSpeed = 0f;
    public int maxHealth = 0;
    public float attackDamage = 0f;
    public float attackRate = 0f;
    public float swapDuration = 0f;
}

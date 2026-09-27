using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Scriptable Objects/PlayerBaseStats")]
public class PlayerBaseStats : ScriptableObject
{
    public float moveSpeed = 5f;
    public int maxHealth = 100;
    public float attackDamage = 6f;
    public float attackRate = 1f;
    public float swapDuration = 1.5f;
}

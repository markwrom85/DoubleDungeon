using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [SerializeField] private PlayerBaseStats baseStats;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public int PlayerId { get; private set; }
    public float MoveSpeed { get; private set; }
    public int MaxHealth { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackRate { get; private set; }

    private void Awake()
    {
        MoveSpeed = baseStats.moveSpeed;
        MaxHealth = baseStats.maxHealth;
        AttackDamage = baseStats.attackDamage;
        AttackRate = baseStats.attackRate;
    }

    private void Start(){
                switch (PlayerId)
        {
            case 1:
                gameObject.name = "Player 1";
                spriteRenderer.color = Color.cyan;
                break;
            case 2:
                gameObject.name = "Player 2";
                spriteRenderer.color = Color.red;
                break;
            case 3:
                gameObject.name = "Player 3";
                spriteRenderer.color = Color.green;
                break;
            case 4:
                gameObject.name = "Player 4";
                spriteRenderer.color = Color.yellow;
                break;
            default:
                gameObject.name = "Player";
                break;
        }
    }

    public void SetPlayerId(int playerId)
    {
        PlayerId = playerId;
    }

    public void ChangeDamage(float amount)
    {
        AttackDamage += amount;
    }
    public void ChangeMoveSpeed(float amount)
    {
        MoveSpeed += amount;
    }
    public void ChangeMaxHealth(int amount)
    {
        MaxHealth += amount;
    }
    public void ChangeAttackRate(float amount)
    {
        AttackRate += amount;
    }
}

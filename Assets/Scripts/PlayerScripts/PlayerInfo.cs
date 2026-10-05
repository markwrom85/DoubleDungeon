using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private PlayerBaseStats baseStats;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite rightSprite;

    public int PlayerId { get; private set; }
    public float MoveSpeed { get; private set; }
    public int MaxHealth { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackRate { get; private set; }
    public float SwapDuration { get; private set; }

    private float currentHealth;

    private void Awake()
    {
        MoveSpeed = baseStats.moveSpeed;
        MaxHealth = baseStats.maxHealth;
        AttackDamage = baseStats.attackDamage;
        AttackRate = baseStats.attackRate;
        SwapDuration = baseStats.swapDuration;

        currentHealth = MaxHealth;
    }

    private void Start()
    {
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

    public void SetDirection(Vector2 direction)
    {
        // Do nothing when the player is not moving.
        // This keeps the player facing their last direction.
        if (direction.sqrMagnitude <= 0.001f)
            return;

        // Determine whether horizontal or vertical movement is stronger.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Moving left or right.
            if (direction.x > 0)
                spriteRenderer.sprite = rightSprite;
            else
                spriteRenderer.sprite = leftSprite;
        }
        else
        {
            // Moving up or down.
            if (direction.y > 0)
                spriteRenderer.sprite = upSprite;
            else
                spriteRenderer.sprite = downSprite;
        }
    }

    public void ChangeStats(
        float moveSpeed,
        int maxHealth,
        float attackDamage,
        float attackRate,
        float swapDuration)
    {
        MoveSpeed += moveSpeed;
        MaxHealth += maxHealth;
        AttackDamage += attackDamage;
        AttackRate += attackRate;
        SwapDuration += swapDuration;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log(
            $"Player {PlayerId} took {damage} damage. Current health: {currentHealth}"
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Handle player death logic here
    }
}
using System.Runtime.Remoting.Lifetime;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private PlayerBaseStats baseStats;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int PlayerId { get; private set; }
    public float MoveSpeed { get; private set; }
    public int MaxHealth { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackRate { get; private set; }
    public float SwapDuration { get; private set; }

    private float currentHealth;

    // Animator on the Player Sprite child.
    private Animator animator;

    private void Awake()
    {
        MoveSpeed = baseStats.moveSpeed;
        MaxHealth = baseStats.maxHealth;
        AttackDamage = baseStats.attackDamage;
        AttackRate = baseStats.attackRate;
        SwapDuration = baseStats.swapDuration;

        currentHealth = MaxHealth;

        if (spriteRenderer != null)
        {
            animator = spriteRenderer.GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning(
                "PlayerInfo could not find an Animator on the assigned Sprite Renderer.",
                this
            );
        }
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
        if (animator == null)
            return;

        bool isMoving = direction.sqrMagnitude > 0.001f;

        // Keep the IsMoving parameter updated.
        animator.SetBool("IsMoving", isMoving);

        // Play the animation while moving and freeze it when stopped.
        animator.speed = isMoving ? 1f : 0f;

        // Do not change the direction when the player is stopped.
        if (!isMoving)
            return;

        // Determine which of the four cardinal directions
        // the player is currently moving.
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0)
            {
                animator.SetInteger("Direction", 3); // Right
            }
            else
            {
                animator.SetInteger("Direction", 2); // Left
            }
        }
        else
        {
            if (direction.y > 0)
            {
                animator.SetInteger("Direction", 1); // Up
            }
            else
            {
                animator.SetInteger("Direction", 0); // Down
            }
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

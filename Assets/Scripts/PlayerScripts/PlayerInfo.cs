
using System.Runtime.Remoting.Lifetime;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private PlayerBaseStats baseStats;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Underglow Sprites")]
    [SerializeField] private SpriteRenderer underglowRenderer;
    [SerializeField] private Sprite player1UnderglowSprite;
    [SerializeField] private Sprite player2UnderglowSprite;
    [SerializeField] private Sprite player3UnderglowSprite;
    [SerializeField] private Sprite player4UnderglowSprite;

    public int PlayerId { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MoveSpeed { get; private set; }
    public int MaxHealth { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackRate { get; private set; }
    public float AOEDamage {get; private set;}
    public float SwapCooldown {get; private set;}
    public float SwapDuration { get; private set; }

    private float currentHealth;

    // Animator on the Player Sprite child.
    private Animator animator;

    private PlayerPanel playerPanel;

    private void Awake()
    {
        MoveSpeed = baseStats.moveSpeed;
        MaxHealth = baseStats.maxHealth;
        AttackDamage = baseStats.attackDamage;
        AttackRate = baseStats.attackRate;
        AOEDamage = baseStats.AOEDamage;
        SwapCooldown = baseStats.swapCooldown;
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
        ApplyPlayerIdentity();
        RefreshHealthDisplay();
    }

    private void ApplyPlayerIdentity()
    {
        switch (PlayerId)
        {
            case 1:
                gameObject.name = "Player 1";
                playerPanel = GameObject.Find("Player 1 Panel")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.cyan;
                if (underglowRenderer != null) underglowRenderer.sprite = player1UnderglowSprite;
                break;

            case 2:
                gameObject.name = "Player 2";
                playerPanel = GameObject.Find("Player 2 Panel")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.red;
                if (underglowRenderer != null) underglowRenderer.sprite = player2UnderglowSprite;
                break;

            case 3:
                gameObject.name = "Player 3";
                playerPanel = GameObject.Find("Player 3 Panel")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.green;
                if (underglowRenderer != null) underglowRenderer.sprite = player3UnderglowSprite;
                break;

            case 4:
                gameObject.name = "Player 4";
                playerPanel = GameObject.Find("Player 4 Panel")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.yellow;
                if (underglowRenderer != null) underglowRenderer.sprite = player4UnderglowSprite;
                break;

            default:
                gameObject.name = "Player";
                break;
        }
    }

    public void SetPlayerId(int playerId)
    {
        if (playerId < 1)
        {
            Debug.LogWarning("Player IDs must start at 1.", this);
            return;
        }

        PlayerId = playerId;
        ApplyPlayerIdentity();
        RefreshHealthDisplay();
    }

    private void RefreshHealthDisplay()
    {
        playerPanel?.ChangeHealthDisplay(currentHealth, MaxHealth);
    }

    public void ChangePanelSide()
    {
        playerPanel?.ChangePanelSide();
    }

    public void SetPanelSide(bool isLeftSide)
    {
        playerPanel?.SetPanelSide(isLeftSide);
    }

    public void SetTeleportCooldownProgress(float progress)
    {
        playerPanel?.SetTeleportCooldownProgress(progress);
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

    // Set whether the player is shooting.
    public void SetShooting(bool isShooting)
    {
        if (animator == null)
            return;

        animator.SetBool("IsShooting", isShooting);
    }

    public void ChangeStats(
        float moveSpeed,
        int maxHealth,
        float attackDamage,
        float attackRate,
        float aoeDamage,
        float swapCooldown,
        float swapDuration)
    {
        MoveSpeed += moveSpeed;
        MaxHealth += maxHealth;
        currentHealth = Mathf.Min(currentHealth + maxHealth, MaxHealth);
        AttackDamage += attackDamage;
        AttackRate += attackRate;
        AOEDamage += aoeDamage;
        SwapCooldown += swapCooldown;
        SwapDuration += swapDuration;
        RefreshHealthDisplay();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshHealthDisplay();

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

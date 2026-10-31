using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private PlayerBaseStats baseStats;
    public SpecialAttack spAttack;

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
    public float AOEDamage { get; private set; }
    public float SwapCooldown { get; private set; }
    public float SwapDuration { get; private set; }

    private float currentHealth;
    private Animator animator;
    private PlayerPanel playerPanel, playerPanel2;

    private int lastDirection;
    private int shootingDirection;
    private bool isShooting;
    private bool isMoving;

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
            animator = spriteRenderer.GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning(
                "PlayerInfo could not find an Animator on the assigned Sprite Renderer.",
                this
            );
        }
        else
        {
            lastDirection = animator.GetInteger("Direction");
            shootingDirection = lastDirection;
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
                playerPanel2 = GameObject.Find("Player 1 Panel 2")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.cyan;
                if (underglowRenderer != null) underglowRenderer.sprite = player1UnderglowSprite;
                break;

            case 2:
                gameObject.name = "Player 2";
                playerPanel = GameObject.Find("Player 2 Panel")?.GetComponent<PlayerPanel>();
                playerPanel2 = GameObject.Find("Player 2 Panel 2")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.red;
                if (underglowRenderer != null) underglowRenderer.sprite = player2UnderglowSprite;
                break;

            case 3:
                gameObject.name = "Player 3";
                playerPanel = GameObject.Find("Player 3 Panel")?.GetComponent<PlayerPanel>();
                playerPanel2 = GameObject.Find("Player 3 Panel 2")?.GetComponent<PlayerPanel>();
                if (spriteRenderer != null) spriteRenderer.color = Color.green;
                if (underglowRenderer != null) underglowRenderer.sprite = player3UnderglowSprite;
                break;

            case 4:
                gameObject.name = "Player 4";
                playerPanel = GameObject.Find("Player 4 Panel")?.GetComponent<PlayerPanel>();
                playerPanel2 = GameObject.Find("Player 4 Panel 2")?.GetComponent<PlayerPanel>();
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
        playerPanel2?.ChangeHealthDisplay(currentHealth, MaxHealth);
    }

    public void ChangePanelSide()
    {
        playerPanel?.ChangePanelSide();
        playerPanel2?.ChangePanelSide();
    }

    public void SetPanelSide(bool isLeftSide)
    {
        playerPanel?.SetPanelSide(isLeftSide);
        playerPanel2?.SetPanelSide(isLeftSide);
    }

    public void SetTeleportCooldownProgress(float progress)
    {
        playerPanel?.SetTeleportCooldownProgress(progress);
        playerPanel2?.SetTeleportCooldownProgress(progress);
    }

    public void SetDirection(Vector2 direction)
    {
        if (animator == null)
            return;

        isMoving = direction.sqrMagnitude > 0.001f;

        if (isMoving)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                lastDirection = direction.x > 0 ? 3 : 2;
            else
                lastDirection = direction.y > 0 ? 1 : 0;
        }

        animator.SetBool("IsMoving", isMoving);

        if (!isShooting)
            animator.SetInteger("Direction", lastDirection);

        animator.speed = (isMoving || isShooting) ? 1f : 0f;
    }

    public void SetShootingDirection(int direction)
    {
        if (animator == null || isShooting)
            return;

        shootingDirection = direction;
    }

    public void SetShooting(bool shooting)
    {
        if (animator == null)
            return;

        if (shooting && !isShooting)
            shootingDirection = lastDirection;

        isShooting = shooting;

        animator.SetBool("IsShooting", isShooting);
        animator.SetInteger(
            "Direction",
            isShooting ? shootingDirection : lastDirection
        );

        animator.speed = (isMoving || isShooting) ? 1f : 0f;
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
            Die();
    }

    public void TriggerSpecialAttack()
    {
        if(spAttack == null) return;
        spAttack.StartAttack();
    }

    private void Die()
    {
    }
}
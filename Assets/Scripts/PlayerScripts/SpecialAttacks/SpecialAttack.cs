using UnityEngine;
using UnityEngine.InputSystem;

public abstract class SpecialAttack : MonoBehaviour
{
    [SerializeField] protected float spDamage, spSize, spLength, spCooldown;
    [Header("Ability Display")]
    [SerializeField] private Sprite panelOutline;
    [SerializeField] private Sprite panelBackground;
    [SerializeField] private Sprite abilityIcon;
    [SerializeField] private RuntimeAnimatorController abilityAnimation;
    protected bool canAttack = true;
    private float readyAt;
    public float CooldownDuration { get; private set; }
    public float CooldownRemaining => canAttack ? 0f : Mathf.Max(0f, readyAt - Time.time);
    public bool IsReady => canAttack;

    protected virtual void Awake()
    {
        PlayerInfo player = GetComponentInParent<PlayerInfo>();
        if (player == null)
        {
            Debug.LogWarning("A special attack must be instantiated as a child of a PlayerInfo object.", this);
            return;
        }

        player.spAttack = this;
        player.ApplySpecialAbilityDisplay(panelOutline, panelBackground, abilityIcon, abilityAnimation);
    }

    protected void BeginCooldown(float duration)
    {
        CooldownDuration = Mathf.Max(0f, duration);
        readyAt = Time.time + CooldownDuration;
        canAttack = false;
    }
    
    protected virtual void Update()
    {
        
    }

    public abstract void StartAttack();
}

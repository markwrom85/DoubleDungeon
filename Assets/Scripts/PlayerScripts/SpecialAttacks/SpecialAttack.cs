using UnityEngine;
using UnityEngine.InputSystem;

public abstract class SpecialAttack : MonoBehaviour
{
    [SerializeField] protected float spDamage, spSize, spLength, spCooldown;
    protected bool canAttack = true;
    private float readyAt;
    public float CooldownDuration { get; private set; }
    public float CooldownRemaining => canAttack ? 0f : Mathf.Max(0f, readyAt - Time.time);
    public bool IsReady => canAttack;

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

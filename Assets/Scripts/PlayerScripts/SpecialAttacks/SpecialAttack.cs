using UnityEngine;
using UnityEngine.InputSystem;

public abstract class SpecialAttack : MonoBehaviour
{
    [SerializeField] protected float spDamage, spSize, spLength, spCooldown;
    protected bool canAttack = true;
    
    protected virtual void Update()
    {
        
    }

    public abstract void StartAttack();
}

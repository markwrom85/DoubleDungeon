// I give damage sources a shared method for damaging a target.
// I let each target handle its own health, damage immunity, and death.
public interface IDamageable
{
    void TakeDamage(float damage);
}

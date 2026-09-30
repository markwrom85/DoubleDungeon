// I define the entry, update, physics update, and exit hooks for enemy states.
public abstract class EnemyState
{
    // I run Enter and Exit on transitions, Tick on updates, and FixedTick on physics updates.
    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void FixedTick() { }
    public virtual void Exit() { }
}

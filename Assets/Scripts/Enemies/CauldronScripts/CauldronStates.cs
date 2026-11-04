public sealed class CauldronReloadState : EnemyState
{
    private readonly CauldronController cauldron;
    public CauldronReloadState(CauldronController cauldron) { this.cauldron = cauldron; }
    public override void Enter() { cauldron.BeginReload(); }
    public override void Tick()
    {
        if (!cauldron.PrepareRoom()) { cauldron.StopMoving(); return; }
        cauldron.RefreshTarget();
        if (cauldron.CanAttack) cauldron.ChangeState(cauldron.AttackState);
    }
    public override void FixedTick() { cauldron.MoveDuringReload(); }
    public override void Exit() { cauldron.StopMoving(); }
}

public sealed class CauldronAttackState : EnemyState
{
    private readonly CauldronController cauldron;
    public CauldronAttackState(CauldronController cauldron) { this.cauldron = cauldron; }
    public override void Enter() { cauldron.BeginAttack(); }
    public override void Tick()
    {
        if (!cauldron.PrepareRoom() || !cauldron.HasTarget)
        { cauldron.ChangeState(cauldron.ReloadState); return; }
        if (cauldron.UpdateAttack()) cauldron.ChangeState(cauldron.ReloadState);
    }
}

public sealed class CauldronLaunchState : EnemyState
{
    private readonly CauldronController cauldron;
    public CauldronLaunchState(CauldronController cauldron) { this.cauldron = cauldron; }
    public override void Enter() { cauldron.BeginLaunch(); }
    public override void Tick()
    {
        if (cauldron.UpdateLaunch()) cauldron.ChangeState(cauldron.SlamState);
    }
}

public sealed class CauldronSlamState : EnemyState
{
    private readonly CauldronController cauldron;
    public CauldronSlamState(CauldronController cauldron) { this.cauldron = cauldron; }
    public override void Enter()
    {
        if (!cauldron.BeginSlam()) cauldron.ChangeState(cauldron.DespawnedState);
    }
    public override void Tick()
    {
        if (cauldron.UpdateSlam()) cauldron.ChangeState(cauldron.DespawnedState);
    }
}

public sealed class CauldronDespawnedState : EnemyState
{
    private readonly CauldronController cauldron;
    public CauldronDespawnedState(CauldronController cauldron) { this.cauldron = cauldron; }
    public override void Enter() { cauldron.CompleteDeath(); }
}

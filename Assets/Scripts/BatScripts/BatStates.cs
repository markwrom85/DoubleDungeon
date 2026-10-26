// Recovery, evasion, returning to the edge, and the windup share one movement state.
public sealed class BatRoamState : EnemyState
{
    private readonly BatController bat;
    public BatRoamState(BatController bat) { this.bat = bat; }
    public override void Enter() { bat.BeginRecovery(); }
    public override void Tick()
    {
        if (bat.UpdateRecovery()) bat.ChangeState(bat.SwoopState);
    }
    public override void FixedTick() { bat.MoveDuringRecovery(); }
    public override void Exit() { bat.StopMoving(); }
}

public sealed class BatSwoopState : EnemyState
{
    private readonly BatController bat;
    public BatSwoopState(BatController bat) { this.bat = bat; }
    public override void Enter() { bat.BeginSwoop(); }
    // BatController advances the swoop after the cameras finish their LateUpdate.
    public override void Exit() { bat.StopMoving(); }
}

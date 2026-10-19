using UnityEngine;

// I wait for a visible player and then enter the behavior assigned to this body.
public class ArmorGhostIdleState : EnemyState
{
    private readonly ArmorGhostController ghost;
    public ArmorGhostIdleState(ArmorGhostController ghost) { this.ghost = ghost; }
    public override void Enter() { ghost.StopMoving(); ghost.SetIdleColor(); }
    public override void Tick()
    {
        ghost.RefreshTarget();
        if (ghost.HasTarget) ghost.ChangeState(ghost.IsArmored ? (EnemyState)ghost.AdvanceState : ghost.FleeState);
    }
}

// I measure actual movement before allowing another shot, without checking an attack range.
public class ArmorGhostAdvanceState : EnemyState
{
    private readonly ArmorGhostController ghost;
    private Vector2 lastPosition;
    private float travelled;
    public ArmorGhostAdvanceState(ArmorGhostController ghost) { this.ghost = ghost; }
    public override void Enter()
    {
        travelled = 0f;
        lastPosition = ghost.Position;
        ghost.ResetSteering();
        ghost.SetAdvanceColor();
    }
    public override void Tick()
    {
        ghost.RefreshTarget();
        if (!ghost.HasTarget) ghost.ChangeState(ghost.IdleState);
    }
    public override void FixedTick()
    {
        travelled += Vector2.Distance(lastPosition, ghost.Position);
        lastPosition = ghost.Position;
        if (travelled >= ghost.DistanceBetweenShots) { ghost.ChangeState(ghost.AttackState); return; }
        ghost.Move(false);
    }
    public override void Exit() { ghost.StopMoving(); }
}

// I stop for one aimed shot and its recovery before requiring another movement phase.
public class ArmorGhostAttackState : EnemyState
{
    private readonly ArmorGhostController ghost;
    private float fireTime, finishTime;
    private bool hasFired;
    public ArmorGhostAttackState(ArmorGhostController ghost) { this.ghost = ghost; }
    public override void Enter()
    {
        ghost.StopMoving();
        ghost.SetAttackColor();
        hasFired = false;
        fireTime = Time.time + ghost.AttackWindup;
        finishTime = fireTime + ghost.AttackRecovery;
    }
    public override void Tick()
    {
        // I cancel the shot if the selected player hides or leaves before it fires.
        if (!ghost.HasTarget) { ghost.ChangeState(ghost.IdleState); return; }
        if (!hasFired && Time.time >= fireTime) { hasFired = true; ghost.Fire(); }
        if (Time.time >= finishTime) ghost.ChangeState(ghost.AdvanceState);
    }
    public override void FixedTick() { ghost.StopMoving(); }
}

// I keep escaping eligible players regardless of their distance.
public class ArmorGhostFleeState : EnemyState
{
    private readonly ArmorGhostController ghost;
    public ArmorGhostFleeState(ArmorGhostController ghost) { this.ghost = ghost; }
    public override void Enter() { ghost.ResetSteering(); ghost.SetFleeColor(); }
    public override void Tick()
    {
        ghost.RefreshTarget();
        if (!ghost.HasTarget) ghost.ChangeState(ghost.IdleState);
    }
    public override void FixedTick() { ghost.Move(true); }
    public override void Exit() { ghost.StopMoving(); }
}

// I hold both bodies in their visible death state until the pair's delay finishes.
public class ArmorGhostDyingState : EnemyState
{
    private readonly ArmorGhostController ghost;
    public ArmorGhostDyingState(ArmorGhostController ghost) { this.ghost = ghost; }
    public override void Enter() { ghost.BeginDying(); }
}

// I hide the body when its pair finishes dying.
public class ArmorGhostDespawnedState : EnemyState
{
    private readonly ArmorGhostController ghost;
    public ArmorGhostDespawnedState(ArmorGhostController ghost) { this.ghost = ghost; }
    public override void Enter() { ghost.StopMoving(); ghost.Hide(); }
}

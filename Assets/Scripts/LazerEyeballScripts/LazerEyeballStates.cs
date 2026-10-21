using UnityEngine;

// I wait for a visible player to enter my room before moving into my section.
public class LazerEyeballIdleState : EnemyState
{
    private readonly LazerEyeballController eye;
    public LazerEyeballIdleState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter() { eye.StopMoving(); eye.HideLaser(); eye.SetIdleColor(); }
    public override void Tick()
    {
        if (!eye.PrepareRoom()) return;
        eye.RefreshTarget();
        if (!eye.CanMove) return;
        eye.ChangeState(eye.IsInMovementArea ? (EnemyState)eye.MoveState : eye.MoveToSideState);
    }
}

// I travel into the outer third while an eligible player is visible.
public class LazerEyeballMoveToSideState : EnemyState
{
    private readonly LazerEyeballController eye;
    public LazerEyeballMoveToSideState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter() { eye.ResetSteering(); eye.SetMovingColor(); }
    public override void Tick()
    {
        eye.RefreshTarget(); eye.Aim(false);
        if (!eye.CanMove) eye.ChangeState(eye.IdleState);
        else if (eye.IsInMovementArea) eye.ChangeState(eye.MoveState);
    }
    public override void FixedTick() { eye.Move(true); }
    public override void Exit() { eye.StopMoving(); }
}

// I roam for a timed phase and keep moving until I can aim an inward shot.
public class LazerEyeballMoveState : EnemyState
{
    private readonly LazerEyeballController eye;
    private float finishTime;
    public LazerEyeballMoveState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter()
    { finishTime = Time.time + eye.MoveDuration; eye.ResetSteering(); eye.SetMovingColor(); }
    public override void Tick()
    {
        eye.RefreshTarget(); eye.Aim(false);
        if (!eye.CanMove) eye.ChangeState(eye.IdleState);
        else if (Time.time >= finishTime && eye.CanShoot) eye.ChangeState(eye.ChargeState);
    }
    public override void FixedTick() { eye.Move(false); }
    public override void Exit() { eye.StopMoving(); }
}

// I hold still while rotating my warning beam toward an eligible player.
public class LazerEyeballChargeState : EnemyState
{
    private readonly LazerEyeballController eye;
    private float finishTime;
    public LazerEyeballChargeState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter()
    { eye.StopMoving(); eye.SetChargingColor(); finishTime = Time.time + eye.ChargeDuration; }
    public override void Tick()
    {
        eye.RefreshTarget();
        if (!eye.CanMove) { eye.ChangeState(eye.IdleState); return; }
        if (!eye.CanShoot) { eye.ChangeState(eye.MoveState); return; }
        eye.Aim(true);
        if (Time.time >= finishTime) eye.ChangeState(eye.FireState);
    }
    public override void Exit() { eye.HideLaser(); }
}

// I keep my aim fixed for this firing cycle, even when the target moves or teleports away.
public class LazerEyeballFireState : EnemyState
{
    private readonly LazerEyeballController eye;
    private float finishTime;
    public LazerEyeballFireState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter()
    { eye.SetFiringColor(); eye.BeginFire(); finishTime = Time.time + eye.FireDuration; }
    public override void Tick() { if (Time.time >= finishTime) eye.ChangeState(eye.RecoveryState); }
    public override void Exit() { eye.HideLaser(); }
}

// I pause after my beam ends before beginning another movement phase.
public class LazerEyeballRecoveryState : EnemyState
{
    private readonly LazerEyeballController eye;
    private float finishTime;
    public LazerEyeballRecoveryState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter()
    { eye.StopMoving(); eye.SetRecoveryColor(); finishTime = Time.time + eye.RecoveryDuration; }
    public override void Tick()
    {
        eye.RefreshTarget();
        if (Time.time >= finishTime) eye.ChangeState(eye.CanMove ? (EnemyState)eye.MoveState : eye.IdleState);
    }
}

// I show my red death state once, then despawn permanently for this life.
public class LazerEyeballDyingState : EnemyState
{
    private readonly LazerEyeballController eye;
    private float finishTime;
    public LazerEyeballDyingState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter() { eye.BeginDying(); finishTime = Time.time + eye.DeathDuration; }
    public override void Tick() { if (Time.time >= finishTime) eye.ChangeState(eye.DespawnedState); }
}

// I disable my body after the death delay so a spawner can reuse it for a fresh enemy.
public class LazerEyeballDespawnedState : EnemyState
{
    private readonly LazerEyeballController eye;
    public LazerEyeballDespawnedState(LazerEyeballController eye) { this.eye = eye; }
    public override void Enter()
    {
        eye.CompleteDeath();
        eye.Despawn();
    }
}

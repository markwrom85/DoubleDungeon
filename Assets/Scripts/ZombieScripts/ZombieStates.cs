using UnityEngine;

// I chase eligible players and enter the attack state when they are in range.
public class ZombieChaseState : EnemyState
{
    private readonly ZombieController zombie;
    public ZombieChaseState(ZombieController zombie) { this.zombie = zombie; }
    public override void Tick()
    {
        zombie.RefreshTarget();
        if (zombie.IsTargetInRange()) zombie.ChangeState(zombie.AttackState);
    }
    public override void FixedTick() { zombie.MoveTowardsTarget(); }
    public override void Exit() { zombie.StopMoving(); }
}

// I wait through a windup, check one strike, and finish the recovery before chasing again.
public class ZombieAttackState : EnemyState
{
    private readonly ZombieController zombie;
    private float strikeTime, finishTime;
    private bool hasStruck;
    public ZombieAttackState(ZombieController zombie) { this.zombie = zombie; }
    public override void Enter()
    {
        zombie.StopMoving();
        zombie.SetColor(zombie.AttackingColor);
        hasStruck = false;
        strikeTime = Time.time + zombie.AttackWindup;
        finishTime = strikeTime + zombie.AttackRecovery;
    }
    public override void Tick()
    {
        if (!zombie.HasTarget) { zombie.ChangeState(zombie.ChaseState); return; }
        if (!hasStruck && Time.time >= strikeTime)
        {
            // I mark the strike before notifying listeners so this attack can only hit once.
            hasStruck = true;
            zombie.CompleteAttack();
        }
        // I return to chase only if the strike has left this attack state active.
        if (zombie.CurrentState == this && Time.time >= finishTime) zombie.ChangeState(zombie.ChaseState);
    }
    public override void FixedTick() { zombie.StopMoving(); }
    public override void Exit() { zombie.SetColor(zombie.NormalColor); }
}

// I keep the zombie visible at its death position until the death delay ends.
public class ZombieDyingState : EnemyState
{
    private readonly ZombieController zombie;
    private float finishTime;
    public ZombieDyingState(ZombieController zombie) { this.zombie = zombie; }
    public override void Enter()
    {
        zombie.SetColor(zombie.DyingColor);
        zombie.StopMoving();
        zombie.SetCollision(false);
        finishTime = Time.time + zombie.DeathDuration;
    }
    public override void Tick()
    {
        if (Time.time < finishTime) return;
        // I resurrect after the first death and deactivate after the second death.
        zombie.ChangeState(zombie.HasResurrected ? (EnemyState)zombie.DespawnedState : zombie.ResurrectingState);
    }
}

// I hide the zombie during transfer, then show it and wait before resuming combat.
public class ZombieResurrectingState : EnemyState
{
    private readonly ZombieController zombie;
    private float retryTime, finishTime;
    private bool hasMoved;
    public ZombieResurrectingState(ZombieController zombie) { this.zombie = zombie; }
    public override void Enter()
    {
        zombie.SetColor(zombie.TransferColor);
        hasMoved = false;
        zombie.SetVisible(false);
        retryTime = Time.time + zombie.TransferDuration;
    }
    public override void Tick()
    {
        if (!hasMoved)
        {
            if (Time.time < retryTime) return;
            // I retry missing side references every half second while keeping the zombie hidden.
            retryTime = Time.time + 0.5f;
            if (!zombie.TryResurrect()) return;
            hasMoved = true;
            zombie.SetColor(zombie.TransferColor);
            zombie.SetVisible(true);
            // I start the movement delay when the zombie reappears on its new side.
            finishTime = Time.time + zombie.ResurrectionDuration;
        }
        if (Time.time < finishTime) return;
        zombie.SetCollision(true);
        zombie.ChangeState(zombie.ChaseState);
    }

    public override void Exit()
    {
        zombie.SetColor(zombie.NormalColor);
    }
}

// I deactivate the zombie when its second death is complete.
public class ZombieDespawnedState : EnemyState
{
    private readonly ZombieController zombie;
    public ZombieDespawnedState(ZombieController zombie) { this.zombie = zombie; }
    public override void Enter() { zombie.gameObject.SetActive(false); }
}

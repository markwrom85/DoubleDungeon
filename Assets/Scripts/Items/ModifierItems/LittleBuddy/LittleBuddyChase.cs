using UnityEngine;

public class LittleBuddyChase : LittleBuddyState
{
    public LittleBuddyChase(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.red;

        lbsc.GetClosestEnemy();
    }

    public override void CheckTransitions()
    {
        if (lbsc.AttackRequested && lbsc.canAttack)
        {
            lbsc.SetState(new LittleBuddyExplode(lbsc));
        }

        if (lbsc.targetEnemy == null || !lbsc.targetEnemy.IsAlive)
        {
            lbsc.SetState(new LittleBuddyReturn(lbsc));
        }

        else if (lbsc.combatManager != null && !lbsc.combatManager.IsInCombat)
        {
            lbsc.SetState(new LittleBuddyIdle(lbsc));
        }
    }

    public override void Act()
    {

    }

    public override void FixedAct()
    {
        Rigidbody2D body = lbsc.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            return;
        }

        if (lbsc.targetEnemy == null || !lbsc.targetEnemy.IsAlive)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 offset = lbsc.targetEnemy.transform.position - body.transform.position;

        body.linearVelocity = offset.normalized * lbsc.chaseSpeed;
    }
}

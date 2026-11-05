using UnityEngine;

public class LittleBuddyReturn : LittleBuddyState
{
    public LittleBuddyReturn(LittleBuddyStateController lbsc) : base(lbsc) { }

    private Vector2 playerPosition => lbsc.playerInfo.transform.position;
    private Vector2 offset;

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.blue;
        Debug.Log("Little Buddy Return State Entered");
    }

    public override void CheckTransitions()
    {
        if (lbsc.combatManager != null && !lbsc.combatManager.IsInCombat)
        {
            lbsc.SetState(new LittleBuddyIdle(lbsc));
        }

        if (lbsc.canAttack && lbsc.combatManager != null && lbsc.combatManager.IsInCombat)
        {
            lbsc.SetState(new LittleBuddyChase(lbsc));
        }

        if (offset.magnitude <= lbsc.returnRange)
        {
            lbsc.SetState(new LittleBuddyCooldown(lbsc));
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

        if (lbsc.playerInfo == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        offset = playerPosition - body.position;
        float stopDistance = Mathf.Max(0f, lbsc.followDistance);
        float slowDistance = Mathf.Max(0f, lbsc.followSlowDistance);
        float distance = offset.magnitude;

        if (distance <= stopDistance)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        float speed = Mathf.SmoothStep(
            0f,
            Mathf.Max(0f, lbsc.returnSpeed),
            Mathf.InverseLerp(stopDistance, stopDistance + slowDistance, distance));

        body.linearVelocity = offset.normalized * lbsc.returnSpeed;
    }

    public override void OnStateExit()
    {
        
    }
}

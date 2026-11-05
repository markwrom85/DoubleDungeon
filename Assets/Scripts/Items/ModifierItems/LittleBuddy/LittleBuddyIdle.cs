using UnityEngine;

public class LittleBuddyIdle : LittleBuddyState
{
    public LittleBuddyIdle(LittleBuddyStateController lbsc) : base(lbsc) { }

    private Vector2 playerPosition => lbsc.playerInfo.transform.position;

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.green;
    }

    public override void CheckTransitions()
    {
        if(lbsc.combatManager != null && lbsc.combatManager.IsInCombat)
        {
            lbsc.SetState(new LittleBuddyChase(lbsc));
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

        Vector2 offset = playerPosition - body.position;
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
            Mathf.Max(0f, lbsc.followSpeed),
            Mathf.InverseLerp(stopDistance, stopDistance + slowDistance, distance));

        body.linearVelocity = offset.normalized * speed;
    }

    public override void OnStateExit()
    {
        Rigidbody2D body = lbsc.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}

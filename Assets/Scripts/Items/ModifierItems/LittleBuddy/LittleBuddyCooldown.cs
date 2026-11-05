using UnityEngine;

public class LittleBuddyCooldown : LittleBuddyState
{
    public LittleBuddyCooldown(LittleBuddyStateController lbsc) : base(lbsc) { }
    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.cyan;
        lbsc.StartCoroutine(lbsc.AttackCooldown(lbsc.attackCooldown));

        lbsc.SetState(new LittleBuddyIdle(lbsc));
    }

    public override void CheckTransitions()
    {

    }

    public override void Act()
    {

    }

    public override void FixedAct()
    {

    }
}
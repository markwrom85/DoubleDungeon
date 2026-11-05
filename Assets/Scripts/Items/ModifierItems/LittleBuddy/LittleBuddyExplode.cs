using UnityEngine;

public class LittleBuddyExplode : LittleBuddyState
{
    public LittleBuddyExplode(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.yellow;
        lbsc.StartCoroutine(lbsc.AttackCooldown(lbsc.attackCooldown));
    }

    public override void CheckTransitions()
    {
        if(lbsc.transitionToReturn)
        {
            lbsc.SetState(new LittleBuddyReturn(lbsc));
        }
    }

    public override void Act()
    {
        // lbsc.StartCoroutine(lbsc.AttackEnemy());
    }

    public override void FixedAct()
    {
        
    }

    public override void OnStateExit()
    {
        lbsc.transitionToReturn = false;
    }
}

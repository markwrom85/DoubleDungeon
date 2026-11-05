using UnityEngine;

public class LittleBuddyExplode : LittleBuddyState
{
    public LittleBuddyExplode(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.yellow;
        Debug.Log("Little Buddy Explode State Entered");
        lbsc.StartAttack();
    }

    public override void CheckTransitions()
    {
        if (lbsc.targetEnemy == null || !lbsc.targetEnemy.IsAlive || lbsc.transitionToReturn)
        {
            lbsc.SetState(new LittleBuddyReturn(lbsc));
        }
    }

    public override void Act()
    {
        
    }

    public override void FixedAct()
    {

    }

    public override void OnStateExit()
    {
        lbsc.transitionToReturn = false;
        Debug.Log("Little Buddy Explode State Exited");
    }
}

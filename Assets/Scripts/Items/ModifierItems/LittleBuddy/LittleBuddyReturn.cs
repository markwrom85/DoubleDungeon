using UnityEngine;

public class LittleBuddyReturn : LittleBuddyState
{
    public LittleBuddyReturn(LittleBuddyStateController lbsc) : base(lbsc) { }

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
    }

    public override void Act()
    {

    }

    public override void FixedAct()
    {
        
    }

    public override void OnStateExit()
    {
        lbsc.StartCoroutine(lbsc.AttackCooldown(lbsc.attackCooldown));
        Debug.Log("Little Buddy Return State Exited");
    }
}

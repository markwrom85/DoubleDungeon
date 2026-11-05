using UnityEngine;

public class LittleBuddyChase : LittleBuddyState
{
    private EnemyController targetEnemy;
    
    public LittleBuddyChase(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.red;
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
}

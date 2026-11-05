using Unity.VisualScripting;
using UnityEngine;

public class LittleBuddyTeleport : LittleBuddyState
{
    public LittleBuddyTeleport(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.gameObject.transform.position = lbsc.playerInfo.gameObject.transform.position;
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

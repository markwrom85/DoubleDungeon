using UnityEngine;

public class LittleBuddyIdle : LittleBuddyState
{
    public LittleBuddyIdle(LittleBuddyStateController lbsc) : base(lbsc)
    {
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
    }

    public override void CheckTransitions()
    {
        // Implement transition logic here
    }

    public override void Act()
    {
        // Implement idle behavior here
    }
}

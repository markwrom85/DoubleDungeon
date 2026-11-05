using UnityEngine;

[System.Serializable]
public abstract class LittleBuddyState
{
    protected LittleBuddyStateController lbsc;

    public abstract void CheckTransitions();
    public abstract void Act();
    public abstract void FixedAct();
    public virtual void OnStateEnter() { }
    public virtual void OnStateExit() { }

    public LittleBuddyState(LittleBuddyStateController lbsc)
    {
        this.lbsc = lbsc;
    }
}

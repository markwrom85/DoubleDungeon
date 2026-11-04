using UnityEngine;

public class LittleBuddyIdle : LittleBuddyState
{
    public LittleBuddyIdle(LittleBuddyStateController lbsc) : base(lbsc) { }

    public override void OnStateEnter()
    {
        lbsc.GetComponent<SpriteRenderer>().color = Color.green;
    }

    public override void CheckTransitions()
    {
        if(lbsc.combatManager != null && lbsc.combatManager.IsInCombat)
        {
            lbsc.SetState(new LittleBuddyCombat(lbsc));
        }
    }

    public override void Act()
    {
        if (lbsc.transform.parent == null)
            return;

        Vector3 offset = lbsc.transform.localPosition;
        if (offset.sqrMagnitude < 0.0001f)
            offset = Vector3.right;

        offset = Quaternion.Euler(0f, 0f, lbsc.speed * Time.deltaTime) * offset;
        lbsc.transform.localPosition = offset;
    }
}

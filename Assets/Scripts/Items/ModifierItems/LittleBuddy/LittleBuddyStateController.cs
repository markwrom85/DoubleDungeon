using System.Collections;
using UnityEngine;

public class LittleBuddyStateController : MonoBehaviour
{
    [Header("Little Buddy Stats")]
    public LittleBuddyState currentState;
    public PlayerInfo playerInfo;
    public CombatManager combatManager;
    public float followSpeed = 5f, followDistance = 2, followSlowDistance = 2;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInfo = GetComponentInParent<PlayerInfo>();

        SetState(new LittleBuddyIdle(this));

        combatManager = FindAnyObjectByType<CombatManager>();

        transform.SetParent(null);
    }

    // Update is called once per frame
    void Update()
    {
        currentState.CheckTransitions();
        currentState.Act();
    }

    void FixedUpdate()
    {
        currentState.FixedAct();
    }

    public void SetState(LittleBuddyState lbs)
    {
        if (currentState != null)
        {
            currentState.OnStateExit();
        }
        currentState = lbs;
        if (currentState != null)
        {
            currentState.OnStateEnter();
        }
    }
}

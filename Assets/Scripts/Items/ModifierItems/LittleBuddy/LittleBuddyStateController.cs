using UnityEngine;

public class LittleBuddyStateController : MonoBehaviour
{
    [SerializeField] private LittleBuddyState currentState;
    [SerializeField] private float damage, speed;
    private PlayerInfo playerInfo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInfo = GetComponentInParent<PlayerInfo>();
        SetState(new LittleBuddyIdle(this));
    }

    // Update is called once per frame
    void Update()
    {
        currentState.CheckTransitions();
        currentState.Act();
    }

    public void SetState(LittleBuddyState lbs)
    {
        if(currentState != null)
        {
            currentState.OnStateExit();
        }
        currentState = lbs;
        if(currentState != null)
        {
            currentState.OnStateEnter();
        }
    }
}

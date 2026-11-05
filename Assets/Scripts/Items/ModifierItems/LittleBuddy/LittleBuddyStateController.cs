using System.Collections;
using UnityEngine;

public class LittleBuddyStateController : MonoBehaviour
{
    [Header("Little Buddy Stats")]
    public LittleBuddyState currentState;
    public PlayerInfo playerInfo;
    public CombatManager combatManager;
    public float followSpeed = 5f, followDistance = 2, followSlowDistance = 2, returnSpeed = 1.5f;

    [Header("Buddy Combat")]
    public float attackDamage = 7f, attackCooldown = 3f, attackDuration = 1f, chaseRange = 10f, chaseSpeed = 7f, returnRange = 1f;
    public AOEBurst buddyAOE;
    public bool canAttack = true, transitionToReturn = false;
    public bool AttackRequested => enemyContacts > 0;
    public EnemyController targetEnemy;
    private int enemyContacts = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInfo = GetComponentInParent<PlayerInfo>();
        combatManager = FindAnyObjectByType<CombatManager>();

        SetState(new LittleBuddyIdle(this));

        transform.SetParent(null);

        playerInfo.PlayerTeleported += BuddyTeleport;
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

    public EnemyController GetClosestEnemy()
    {
        targetEnemy = null;
        if (combatManager == null)
        {
            return null;
        }

        float range = Mathf.Max(0f, chaseRange);
        float closestDistanceSqr = range * range;
        Vector2 buddyPosition = transform.position;

        foreach (EnemyController enemy in combatManager.ActiveEnemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
            {
                continue;
            }

            float distanceSqr = ((Vector2)enemy.transform.position - buddyPosition).sqrMagnitude;
            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                targetEnemy = enemy;
            }
        }

        return targetEnemy;
    }

    public void StartAttackCooldown()
    {
        StartCoroutine(AttackCooldown(attackCooldown));
    }

    public IEnumerator AttackCooldown(float cooldown)
    {
        canAttack = false;
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }

    public void StartAttack()
    {
        if (!canAttack)
            return;

        canAttack = false;
        StartCoroutine(AttackEnemy());
    }

    public IEnumerator AttackEnemy()
    {
        buddyAOE.gameObject.SetActive(true);
        buddyAOE.aoeDamage = attackDamage;
        yield return new WaitForSeconds(attackDuration);
        buddyAOE.gameObject.SetActive(false);
        transitionToReturn = true;
    }

    public void BuddyTeleport()
    {
        Debug.Log("buddy teleport");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<EnemyController>() != null)
        {
            enemyContacts++;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<EnemyController>() != null)
        {
            enemyContacts = Mathf.Max(0, enemyContacts - 1);
        }
    }
}

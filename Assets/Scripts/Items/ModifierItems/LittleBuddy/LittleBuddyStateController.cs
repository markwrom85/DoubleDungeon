using System.Collections;
using UnityEngine;

public class LittleBuddyStateController : MonoBehaviour
{
    [Header("Little Buddy Stats")]
    public LittleBuddyState currentState;
    public PlayerInfo playerInfo;
    public CombatManager combatManager;
    public float damage, speed;

    [Header("Little Buddy Combat")]
    public Bullet bulletPrefab;
    public float shootCooldown = 1.0f, enemyDetectionRange = 10.0f;
    public bool canShoot = true;

    private BulletPool bulletPool;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInfo = GetComponentInParent<PlayerInfo>();

        SetState(new LittleBuddyIdle(this));

        combatManager = FindAnyObjectByType<CombatManager>();

        if (bulletPrefab == null && playerInfo != null)
            bulletPrefab = playerInfo.GetComponentInChildren<CardinalGun>()?.bulletPrefab;

        if (bulletPrefab != null)
            bulletPool = new BulletPool(bulletPrefab, 4, 16, gameObject.scene);
    }

    // Update is called once per frame
    void Update()
    {
        currentState.CheckTransitions();
        currentState.Act();
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

    public EnemyController FindClosestEnemy()
    {
        if (combatManager == null)
            return null;

        EnemyController closestEnemy = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 buddyPosition = transform.position;
        Vector3 playerPosition = GetPlayerPosition(buddyPosition);
        Vector2 buddySide = GetBuddySide(playerPosition, buddyPosition);

        foreach (var enemy in combatManager.ActiveEnemies)
        {
            if (enemy == null
                || !IsWithinDetectionRange(enemy, buddyPosition)
                || !IsOnBuddySide(enemy, playerPosition, buddySide))
                continue;

            Vector3 directionToEnemy = enemy.transform.position - buddyPosition;
            float dSqrToEnemy = directionToEnemy.sqrMagnitude;

            if (dSqrToEnemy < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToEnemy;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private bool IsWithinDetectionRange(
        EnemyController enemy,
        Vector3 buddyPosition)
    {
        float detectionRange = Mathf.Max(0f, enemyDetectionRange);
        float detectionRangeSqr = detectionRange * detectionRange;
        float distanceToEnemySqr =
            (enemy.transform.position - buddyPosition).sqrMagnitude;

        // Only enemies within the buddy's detection range can be targeted.
        return distanceToEnemySqr <= detectionRangeSqr;
    }

    private Vector3 GetPlayerPosition(Vector3 fallbackPosition)
    {
        return playerInfo != null
            ? playerInfo.transform.position
            : fallbackPosition;
    }

    private Vector2 GetBuddySide(Vector3 playerPosition, Vector3 buddyPosition)
    {
        Vector2 buddySide = buddyPosition - playerPosition;

        // If the buddy is centered on the player, use its right direction.
        if (buddySide.sqrMagnitude < 0.0001f)
            return transform.right;

        return buddySide.normalized;
    }

    private bool IsOnBuddySide(
        EnemyController enemy,
        Vector3 playerPosition,
        Vector2 buddySide)
    {
        Vector2 enemySide = enemy.transform.position - playerPosition;

        // A positive dot product means the enemy is on the same side
        // of the player as the buddy.
        return Vector2.Dot(enemySide, buddySide) > 0f;
    }

    public IEnumerator Shoot(Vector2 direction)
    {
        if (bulletPool == null || direction.sqrMagnitude < 0.0001f)
            yield break;

        canShoot = false;
        Transform owner = transform.root;
        Quaternion rotation = Quaternion.FromToRotation(Vector2.right, direction);

        bulletPool.Fire(transform.position, rotation, direction, owner, damage);

        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    private void OnDestroy()
    {
        bulletPool?.Dispose();
    }
}

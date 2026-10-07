using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
// I control one ArmorGhost body according to the role assigned by its pair.
public class ArmorGhostController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float advanceSpeed = 2f;
    [SerializeField, Min(0.1f)] private float fleeSpeed = 3f;
    [SerializeField, Min(0.1f)] private float distanceBetweenShots = 2f;
    [SerializeField, Min(0.05f)] private float targetInterval = 0.3f;
    [Header("Obstacle Steering")]
    [SerializeField, Min(0.05f)] private float steeringInterval = 0.15f;
    [SerializeField, Min(0.1f)] private float obstacleLookAhead = 1f;
    [SerializeField, Min(0.2f)] private float stuckDuration = 0.7f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Header("Shooting")]
    [SerializeField] private EnemyBullet bulletPrefab;
    [SerializeField, Min(0f)] private float projectileDamage = 10f;
    [SerializeField, Min(0f)] private float attackWindup = 0.4f;
    [SerializeField, Min(0.05f)] private float attackRecovery = 0.5f;
    [SerializeField, Range(0f, 1f)] private float aimLead = 0.35f;
    [SerializeField, Min(0f)] private float maxLeadTime = 0.5f;
    [SerializeField, Min(1)] private int initialPoolSize = 8;
    [SerializeField, Min(1)] private int maxPoolSize = 64;
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color advanceColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color attackColor = new Color(0.7f, 0.2f, 1f);
    [SerializeField] private Color fleeColor = Color.cyan;
    [SerializeField] private Color dyingColor = Color.red;
    [SerializeField] private Color hitColor = Color.white;

    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private SpriteRenderer spriteRenderer;
    private ArmorGhostPair pair;
    private EnemyBulletPool bulletPool;
    private Rigidbody2D targetBody;
    private readonly EnemyStateMachine stateMachine = new EnemyStateMachine();
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[32];
    private ContactFilter2D obstacleFilter;
    private Vector2 moveDirection, progressPosition, wallDirection;
    private float nextTargetTime, nextSteeringTime, progressTime, recoveryUntil, hitFlashUntil, wallFollowUntil;
    private float recoveryTurn = 1f;
    private Color stateColor;

    public EnemyDungeonSide CurrentSide { get; private set; }
    public PlayerInput Target { get; private set; }
    public bool IsArmored { get; private set; }
    public bool HasTarget => CurrentSide != null && CurrentSide.IsValidTarget(Target);
    public Vector2 Position => body.position;
    public float DistanceBetweenShots => distanceBetweenShots;
    public float AttackWindup => attackWindup;
    public float AttackRecovery => attackRecovery;
    public EnemyState CurrentState => stateMachine.CurrentState;
    public string CurrentStateName => CurrentState?.GetType().Name ?? "None";
    public ArmorGhostIdleState IdleState { get; private set; }
    public ArmorGhostAdvanceState AdvanceState { get; private set; }
    public ArmorGhostAttackState AttackState { get; private set; }
    public ArmorGhostFleeState FleeState { get; private set; }
    public ArmorGhostDyingState DyingState { get; private set; }
    public ArmorGhostDespawnedState DespawnedState { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;
        // I create the states and cast buffer once so movement and transitions do not allocate them repeatedly.
        IdleState = new ArmorGhostIdleState(this);
        AdvanceState = new ArmorGhostAdvanceState(this);
        AttackState = new ArmorGhostAttackState(this);
        FleeState = new ArmorGhostFleeState(this);
        DyingState = new ArmorGhostDyingState(this);
        DespawnedState = new ArmorGhostDespawnedState(this);
    }

    public void Initialize(ArmorGhostPair owner, EnemyDungeonSide side, Vector3 position, bool armored)
    {
        StopMoving();
        stateMachine.ChangeState(null);
        pair = owner;
        CurrentSide = side;
        IsArmored = armored;
        Target = null;
        targetBody = null;
        transform.position = position;
        body.position = position;
        body.simulated = true;
        hitbox.enabled = true;
        spriteRenderer.enabled = true;
        hitFlashUntil = 0f;
        nextTargetTime = Time.time + Random.Range(0f, targetInterval);
        ResetSteering();
        if (armored)
            bulletPool = EnemyBulletPool.GetOrCreate(bulletPrefab, gameObject.scene, initialPoolSize, maxPoolSize);
        ChangeState(IdleState);
    }

    private void Update()
    {
        stateMachine.Tick();
        if (hitFlashUntil > 0f && Time.time >= hitFlashUntil)
        {
            hitFlashUntil = 0f;
            spriteRenderer.color = stateColor;
        }
    }
    private void FixedUpdate() { stateMachine.FixedTick(); }
    private void OnDisable()
    {
        StopMoving();
        stateMachine.ChangeState(null);
    }
    public void WaitForRoom()
    {
        // I leave both waiting bodies inactive in physics until the pair supplies their room references.
        StopMoving();
        stateMachine.ChangeState(null);
        Target = null;
        targetBody = null;
        CurrentSide = null;
        pair = null;
        hitFlashUntil = 0f;
        hitbox.enabled = false;
        body.simulated = false;
        spriteRenderer.enabled = false;
    }
    public void ChangeState(EnemyState state) { stateMachine.ChangeState(state); }

    public void RefreshTarget()
    {
        // I clear hidden or departed players immediately and space out searches for a new target.
        if (!HasTarget) { Target = null; targetBody = null; }
        if (Time.time < nextTargetTime) return;
        nextTargetTime = Time.time + targetInterval;
        PlayerInput next = CurrentSide != null ? CurrentSide.FindTarget(body.position) : null;
        if (next == Target) return;
        Target = next;
        targetBody = Target != null ? Target.GetComponent<Rigidbody2D>() : null;
        nextSteeringTime = 0f;
    }

    public void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsArmored || pair == null) return;
        pair.TakeDamage(this, damage);
    }

    public void Fire()
    {
        if (!IsArmored || pair == null || pair.IsDying || !HasTarget || bulletPool == null) return;
        Vector2 aim = Target.transform.position;
        if (targetBody != null && bulletPrefab != null)
        {
            // I lead by a capped fraction of the bullet's estimated travel time.
            float travelTime = Vector2.Distance(body.position, aim) / Mathf.Max(0.1f, bulletPrefab.speed);
            aim += targetBody.linearVelocity * Mathf.Min(maxLeadTime, travelTime * aimLead);
        }
        Vector2 direction = (aim - body.position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        bulletPool.Fire(transform.position, direction, projectileDamage, CurrentSide);
    }

    public void Move(bool fleeing)
    {
        if (!HasTarget) { StopMoving(); return; }
        float speed = fleeing ? fleeSpeed : advanceSpeed;
        if ((body.position - progressPosition).sqrMagnitude >= 0.01f)
        {
            progressPosition = body.position;
            progressTime = Time.time;
        }
        else if (Time.time - progressTime >= stuckDuration)
        {
            // I change my escape bias when I have stopped making progress instead of pushing into the same wall.
            recoveryTurn = -recoveryTurn;
            recoveryUntil = Time.time + stuckDuration;
            progressTime = Time.time;
            nextSteeringTime = 0f;
        }
        if (Time.time >= nextSteeringTime)
        {
            nextSteeringTime = Time.time + steeringInterval;
            Vector2 preferred = fleeing ? GetEscapeDirection() : ((Vector2)Target.transform.position - body.position).normalized;
            if (Time.time < recoveryUntil)
                preferred = new Vector2(-preferred.y, preferred.x) * recoveryTurn;
            moveDirection = FindOpenDirection(preferred);
        }
        // I check every physics step against the area while physics handles contact with solid obstacles.
        float radius = Mathf.Max(hitbox.bounds.extents.x, hitbox.bounds.extents.y);
        Vector2 step = moveDirection * speed * Time.fixedDeltaTime;
        body.linearVelocity = CurrentSide.Contains(body.position + step, radius + 0.02f)
            ? moveDirection * speed : Vector2.zero;
    }

    private Vector2 GetEscapeDirection()
    {
        Vector2 direction = Vector2.zero;
        // I consider all eligible players, with closer players contributing more to the escape direction.
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (!CurrentSide.IsValidTarget(player)) continue;
            Vector2 away = body.position - (Vector2)player.transform.position;
            direction += away.normalized / Mathf.Max(0.25f, away.sqrMagnitude);
        }
        if (direction.sqrMagnitude < 0.0001f)
            direction = body.position - (Vector2)Target.transform.position;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
    }

    private Vector2 FindOpenDirection(Vector2 preferred)
    {
        float bestScore = float.NegativeInfinity;
        Vector2 best = Vector2.zero;
        bool blocked = GetClearance(preferred) < obstacleLookAhead * 0.6f;
        bool followingWall = blocked && Time.time < wallFollowUntil;
        // I compare a small set of directions and favor continuing along an open wall instead of jittering.
        for (int index = 0; index <= 16; index++)
        {
            float angle = index * Mathf.PI / 8f;
            Vector2 candidate = index == 16 ? preferred : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (candidate.sqrMagnitude < 0.0001f) continue;
            float clearance = GetClearance(candidate);
            if (clearance < 0.08f) continue;
            float score = Vector2.Dot(candidate, preferred) * (blocked ? 0.75f : 2f)
                + clearance / obstacleLookAhead * 1.5f
                + Vector2.Dot(candidate, followingWall ? wallDirection : moveDirection) * (followingWall ? 2f : 0.4f);
            if (score <= bestScore) continue;
            bestScore = score;
            best = candidate;
        }
        // I hold an open route briefly when my preferred route is blocked so I can leave corners and round walls.
        if (blocked && !followingWall && best.sqrMagnitude > 0f)
        {
            wallDirection = best;
            wallFollowUntil = Time.time + 0.9f;
        }
        if (!blocked) wallFollowUntil = 0f;
        return best;
    }

    private float GetClearance(Vector2 direction)
    {
        float radius = Mathf.Max(hitbox.bounds.extents.x, hitbox.bounds.extents.y);
        Vector2 origin = hitbox.bounds.center;
        float clearance = obstacleLookAhead;
        int count = Physics2D.CircleCast(origin, radius, direction, obstacleFilter, obstacleHits, clearance);
        for (int index = 0; index < count; index++)
        {
            Collider2D obstacle = obstacleHits[index].collider;
            if (obstacle == hitbox || obstacle.transform.IsChildOf(transform)) continue;
            clearance = Mathf.Min(clearance, Mathf.Max(0f, obstacleHits[index].distance - 0.03f));
        }
        if (!CurrentSide.Contains(body.position + direction * clearance, radius + 0.02f))
        {
            // I shorten a probe at the room edge even when that edge has no physical wall collider.
            float low = 0f, high = clearance;
            for (int index = 0; index < 6; index++)
            {
                float middle = (low + high) * 0.5f;
                if (CurrentSide.Contains(body.position + direction * middle, radius + 0.02f)) low = middle;
                else high = middle;
            }
            clearance = low;
        }
        return clearance;
    }

    public void ResetSteering()
    {
        moveDirection = Vector2.zero;
        nextSteeringTime = 0f;
        recoveryUntil = 0f;
        wallFollowUntil = 0f;
        progressPosition = body.position;
        progressTime = Time.time;
    }
    public void StopMoving() { if (body != null) body.linearVelocity = Vector2.zero; }
    public void SetIdleColor() { SetStateColor(idleColor); }
    public void SetAdvanceColor() { SetStateColor(advanceColor); }
    public void SetAttackColor() { SetStateColor(attackColor); }
    public void SetFleeColor() { SetStateColor(fleeColor); }

    private void SetStateColor(Color color)
    {
        stateColor = color;
        spriteRenderer.color = hitFlashUntil > Time.time ? hitColor : color;
    }
    public void FlashOnHit()
    {
        hitFlashUntil = Time.time + 0.1f;
        spriteRenderer.color = hitColor;
    }
    public void BeginDying()
    {
        StopMoving();
        hitbox.enabled = false;
        body.simulated = false;
        hitFlashUntil = 0f;
        SetStateColor(dyingColor);
    }
    public void Hide() { spriteRenderer.enabled = false; }
}

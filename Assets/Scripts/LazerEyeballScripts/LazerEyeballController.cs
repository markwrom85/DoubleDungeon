using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyDungeonSetup), typeof(LazerEyeballLaser))]
// I move inside the outer part of my room and alternate between aiming and a locked laser shot.
public class LazerEyeballController : EnemyController
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float eyeMaxHealth = 20f;
    [SerializeField, Min(0f)] private float deathDuration = 1f;
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Range(0.1f, 0.5f)] private float roomWidthFraction = 0.3333333f;
    [SerializeField, Min(0.1f)] private float moveDuration = 3f;
    [SerializeField, Min(0.25f)] private float roamDestinationDuration = 3f;
    [SerializeField, Min(0.05f)] private float targetInterval = 0.3f;
    [SerializeField, Min(0.05f)] private float steeringInterval = 0.15f;
    [SerializeField, Min(0.1f)] private float obstacleLookAhead = 1f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Header("Laser")]
    [SerializeField, Min(0f)] private float laserDamage = 10f;
    [SerializeField, Min(0.1f)] private float chargeDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float fireDuration = 1f;
    [SerializeField, Min(0f)] private float recoveryDuration = 1f;
    [SerializeField, Min(1f)] private float rotationSpeed = 120f;
    [SerializeField, Range(0f, 90f)] private float oppositeTargetAngle = 20f;
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color movingColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private Color firingColor = new Color(0.7f, 0.2f, 1f);
    [SerializeField] private Color recoveryColor = Color.cyan;
    [SerializeField] private Color dyingColor = Color.red;
    [SerializeField] private Color hitColor = Color.white;

    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private SpriteRenderer spriteRenderer;
    private EnemyDungeonSetup dungeonSetup;
    private LazerEyeballLaser laser;
    private DungeonManager manager;
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[32];
    private ContactFilter2D obstacleFilter;
    private Rect movementArea;
    private Vector2 moveDirection, progressPosition, wallDirection, roamDestination;
    private float nextTargetTime, nextSteeringTime, progressTime, wallFollowUntil, hitFlashUntil, nextRoamTime;
    private Color stateColor;
    private bool onLeft;
    private bool hasEnteredRoom, hasRoamDestination;

    public EnemyDungeonSide CurrentSide { get; private set; }
    public PlayerInput Target { get; private set; }
    public Vector2 Position => body.position;
    public Vector2 Facing { get; private set; }
    public float MoveDuration => moveDuration;
    public float ChargeDuration => chargeDuration;
    public float FireDuration => fireDuration;
    public float RecoveryDuration => recoveryDuration;
    public float DeathDuration => deathDuration;
    public bool HasTarget => IsValidTarget(Target);
    public bool CanMove => hasEnteredRoom && HasTarget;
    public bool CanShoot => hasEnteredRoom && HasShot(Target);
    public bool IsInMovementArea => movementArea.Contains(body.position);
    public LazerEyeballIdleState IdleState { get; private set; }
    public LazerEyeballMoveToSideState MoveToSideState { get; private set; }
    public LazerEyeballMoveState MoveState { get; private set; }
    public LazerEyeballChargeState ChargeState { get; private set; }
    public LazerEyeballFireState FireState { get; private set; }
    public LazerEyeballRecoveryState RecoveryState { get; private set; }
    public LazerEyeballDyingState DyingState { get; private set; }
    public LazerEyeballDespawnedState DespawnedState { get; private set; }

    protected override void Awake()
    {
        MaxHealth = eyeMaxHealth;
        base.Awake();
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        dungeonSetup = GetComponent<EnemyDungeonSetup>();
        laser = GetComponent<LazerEyeballLaser>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;
        foreach (DungeonManager candidate in FindObjectsByType<DungeonManager>(FindObjectsInactive.Include))
            if (candidate.gameObject.scene == gameObject.scene) { manager = candidate; break; }
        // I create each state and my obstacle buffer once and reuse them throughout my life.
        IdleState = new LazerEyeballIdleState(this);
        MoveToSideState = new LazerEyeballMoveToSideState(this);
        MoveState = new LazerEyeballMoveState(this);
        ChargeState = new LazerEyeballChargeState(this);
        FireState = new LazerEyeballFireState(this);
        RecoveryState = new LazerEyeballRecoveryState(this);
        DyingState = new LazerEyeballDyingState(this);
        DespawnedState = new LazerEyeballDespawnedState(this);
    }

    private void OnEnable()
    {
        Health = MaxHealth;
        CurrentSide = null;
        Target = null;
        hasEnteredRoom = false;
        hitFlashUntil = 0f;
        nextTargetTime = Time.time + Random.Range(0f, targetInterval);
        hitbox.enabled = true;
        body.simulated = true;
        spriteRenderer.enabled = true;
        ResetSteering();
        ChangeState(IdleState);
    }

    protected override void Update()
    {
        // I stop attacking when the cameras leave my original pair, without changing that pair's references.
        if (Health > 0f && CurrentSide != null && !IsRoomActive() && CurrentState != IdleState)
            ChangeState(IdleState);
        base.Update();
        if (hitFlashUntil > 0f && Time.time >= hitFlashUntil)
        { hitFlashUntil = 0f; spriteRenderer.color = stateColor; }
    }
    protected override void FixedUpdate() { base.FixedUpdate(); }
    protected override void OnDisable()
    {
        StopMoving();
        laser.Hide();
        base.OnDisable();
    }

    public bool PrepareRoom()
    {
        if (CurrentSide == null || CurrentSide.OppositeSide == null)
        {
            if (Time.time < nextTargetTime) return false;
            nextTargetTime = Time.time + targetInterval;
            CurrentSide = dungeonSetup.GetStartingSide(body.position);
            if (CurrentSide == null || CurrentSide.OppositeSide == null) return false;
            onLeft = dungeonSetup.GetSide(true) == CurrentSide;
            Facing = onLeft ? Vector2.right : Vector2.left;
            FaceDirection(Facing);
            laser.Configure(CurrentSide, onLeft);
        }
        if (!IsRoomActive()) return false;
        Bounds bounds = CurrentSide.LevelBounds.bounds;
        float margin = Radius + 0.05f;
        float stripWidth = Mathf.Max(margin * 2.1f, bounds.size.x * roomWidthFraction);
        movementArea = Rect.MinMaxRect(onLeft ? bounds.min.x + margin : bounds.max.x - stripWidth + margin,
            bounds.min.y + margin, onLeft ? bounds.min.x + stripWidth - margin : bounds.max.x - margin,
            bounds.max.y - margin);
        return true;
    }

    private bool IsRoomActive()
    {
        if (CurrentSide == null || CurrentSide.OppositeSide == null) return false;
        if (manager == null) return true;
        CinemachineCamera first = onLeft ? manager.leftCinemachineCamera : manager.rightCinemachineCamera;
        CinemachineCamera second = onLeft ? manager.rightCinemachineCamera : manager.leftCinemachineCamera;
        Transform room = CurrentSide.LevelBounds != null ? CurrentSide.LevelBounds.transform.parent : null;
        Transform other = CurrentSide.OppositeSide.LevelBounds != null ? CurrentSide.OppositeSide.LevelBounds.transform.parent : null;
        return first != null && second != null && room != null && other != null
            && first.transform.IsChildOf(room) && second.transform.IsChildOf(other);
    }

    private bool IsValidTarget(PlayerInput player)
    {
        return IsRoomActive() && (CurrentSide.IsValidTarget(player) || CurrentSide.OppositeSide.IsValidTarget(player));
    }

    private bool HasShot(PlayerInput player)
    {
        if (!IsValidTarget(player)) return false;
        Vector2 aim = laser.GetAimDirection(player);
        return aim.sqrMagnitude > 0.0001f && Vector2.Dot(aim, laser.ClampDirection(aim)) >= 0.995f;
    }

    public void RefreshTarget()
    {
        if (!HasTarget) Target = null;
        if (Time.time < nextTargetTime || !IsRoomActive()) return;
        nextTargetTime = Time.time + targetInterval;
        PlayerInput local = null, localShot = null, opposite = null;
        float localDistance = float.PositiveInfinity, shotDistance = float.PositiveInfinity, oppositeAngle = float.PositiveInfinity;
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (!IsValidTarget(player)) continue;
            if (CurrentSide.IsValidTarget(player))
            {
                // I wake up only after a visible player enters my own room, even if they are behind me.
                hasEnteredRoom = true;
                float distance = ((Vector2)player.transform.position - body.position).sqrMagnitude;
                if (distance < localDistance) { local = player; localDistance = distance; }
                if (distance < shotDistance && HasShot(player)) { localShot = player; shotDistance = distance; }
            }
            else if (HasShot(player))
            {
                float angle = Vector2.Angle(Facing, laser.GetAimDirection(player));
                if (angle < oppositeAngle) { opposite = player; oppositeAngle = angle; }
            }
        }
        // I retain a player behind me as a movement target instead of treating an unavailable shot as invisibility.
        if (localShot != null) local = localShot;
        // I favor my own room unless the other player presents a noticeably better aligned inward shot.
        float localAngle = local != null ? Vector2.Angle(Facing, laser.GetAimDirection(local)) : 180f;
        Target = opposite != null && (local == null || !HasShot(local)
            || oppositeAngle <= oppositeTargetAngle && oppositeAngle + 10f < localAngle)
            ? opposite : local;
    }

    public void Aim(bool showWarning)
    {
        Vector2 desired = HasTarget ? laser.GetAimDirection(Target) : onLeft ? Vector2.right : Vector2.left;
        desired = laser.ClampDirection(desired);
        float angle = Mathf.MoveTowardsAngle(Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg,
            Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg, rotationSpeed * Time.deltaTime);
        Facing = laser.ClampDirection(new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)));
        FaceDirection(Facing);
        if (showWarning) laser.ShowWarning(Facing);
    }
    private void FaceDirection(Vector2 aim) { transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg); }
    public void BeginFire() { StopMoving(); laser.BeginFire(Facing, laserDamage); }
    public void HideLaser() { laser.Hide(); }

    public void Move(bool approachingStrip)
    {
        if (!CanMove) { StopMoving(); return; }
        if ((body.position - progressPosition).sqrMagnitude >= 0.01f)
        { progressPosition = body.position; progressTime = Time.time; }
        bool stuck = Time.time - progressTime > 0.7f;
        if (stuck) { nextSteeringTime = 0f; wallFollowUntil = 0f; progressTime = Time.time; hasRoamDestination = false; }
        if (!approachingStrip && (!hasRoamDestination || Time.time >= nextRoamTime
            || (roamDestination - body.position).sqrMagnitude < 0.09f))
            ChooseRoamDestination();
        if (Time.time >= nextSteeringTime)
        {
            nextSteeringTime = Time.time + steeringInterval;
            Vector2 destination = approachingStrip ? new Vector2(movementArea.center.x,
                Mathf.Clamp(body.position.y, movementArea.yMin + 0.05f, movementArea.yMax - 0.05f))
                : roamDestination;
            destination.x = Mathf.Clamp(destination.x, movementArea.xMin + 0.05f, movementArea.xMax - 0.05f);
            destination.y = Mathf.Clamp(destination.y, movementArea.yMin + 0.05f, movementArea.yMax - 0.05f);
            Vector2 preferred = (destination - body.position).normalized;
            if (stuck) preferred = new Vector2(-preferred.y, preferred.x);
            moveDirection = FindOpenDirection(preferred, approachingStrip);
        }
        Vector2 step = moveDirection * moveSpeed * Time.fixedDeltaTime;
        body.linearVelocity = AllowedPosition(body.position + step, approachingStrip) ? moveDirection * moveSpeed : Vector2.zero;
        if (body.linearVelocity == Vector2.zero) nextSteeringTime = 0f;
    }

    private void ChooseRoamDestination()
    {
        // I choose points across the whole section instead of clamping my movement to the player's position at its edge.
        for (int index = 0; index < 8; index++)
        {
            roamDestination = new Vector2(Random.Range(movementArea.xMin + 0.05f, movementArea.xMax - 0.05f),
                Random.Range(movementArea.yMin + 0.05f, movementArea.yMax - 0.05f));
            if ((roamDestination - body.position).sqrMagnitude >= 0.25f) break;
        }
        hasRoamDestination = true;
        nextRoamTime = Time.time + roamDestinationDuration;
        nextSteeringTime = wallFollowUntil = 0f;
    }

    private float Radius => hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
    private bool AllowedPosition(Vector2 point, bool approachingStrip)
    {
        return approachingStrip ? CurrentSide.Contains(point, Radius + 0.02f) : movementArea.Contains(point);
    }

    private Vector2 FindOpenDirection(Vector2 preferred, bool approachingStrip)
    {
        float bestScore = float.NegativeInfinity;
        Vector2 best = Vector2.zero;
        bool blocked = GetClearance(preferred, approachingStrip) < obstacleLookAhead * 0.6f;
        bool followingWall = blocked && Time.time < wallFollowUntil;
        for (int index = 0; index <= 16; index++)
        {
            float angle = index * Mathf.PI / 8f;
            Vector2 candidate = index == 16 ? preferred : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float clearance = GetClearance(candidate, approachingStrip);
            if (clearance < 0.08f) continue;
            float score = Vector2.Dot(candidate, preferred) * (blocked ? 0.75f : 2f)
                + clearance / obstacleLookAhead * 1.5f
                + Vector2.Dot(candidate, followingWall ? wallDirection : moveDirection) * (followingWall ? 2f : 0.4f);
            if (score <= bestScore) continue;
            bestScore = score; best = candidate;
        }
        if (blocked && !followingWall && best.sqrMagnitude > 0f)
        { wallDirection = best; wallFollowUntil = Time.time + 0.9f; }
        if (!blocked) wallFollowUntil = 0f;
        return best;
    }

    private float GetClearance(Vector2 direction, bool approachingStrip)
    {
        if (direction.sqrMagnitude < 0.0001f) return 0f;
        float clearance = obstacleLookAhead;
        int count = Physics2D.CircleCast(body.position, Radius, direction, obstacleFilter, obstacleHits, clearance);
        for (int index = 0; index < count; index++)
        {
            Collider2D obstacle = obstacleHits[index].collider;
            if (obstacle == hitbox || obstacle.transform.IsChildOf(transform)) continue;
            clearance = Mathf.Min(clearance, Mathf.Max(0f, obstacleHits[index].distance - 0.03f));
        }
        if (!AllowedPosition(body.position + direction * clearance, approachingStrip))
        {
            float low = 0f, high = clearance;
            for (int index = 0; index < 6; index++)
            {
                float middle = (low + high) * 0.5f;
                if (AllowedPosition(body.position + direction * middle, approachingStrip)) low = middle;
                else high = middle;
            }
            clearance = low;
        }
        return clearance;
    }

    public void ResetSteering()
    {
        moveDirection = Vector2.zero;
        nextSteeringTime = wallFollowUntil = 0f;
        hasRoamDestination = false;
        progressPosition = body.position;
        progressTime = Time.time;
    }
    public void StopMoving() { if (body != null) body.linearVelocity = Vector2.zero; }
    public override void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || Health <= 0f || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        Health = Mathf.Max(0f, Health - damage);
        if (Health <= 0f) ChangeState(DyingState);
        else { hitFlashUntil = Time.time + 0.1f; spriteRenderer.color = hitColor; }
    }
    public void SetIdleColor() { SetColor(idleColor); }
    public void SetMovingColor() { SetColor(movingColor); }
    public void SetChargingColor() { SetColor(chargingColor); }
    public void SetFiringColor() { SetColor(firingColor); }
    public void SetRecoveryColor() { SetColor(recoveryColor); }
    private void SetColor(Color color)
    { stateColor = color; spriteRenderer.color = hitFlashUntil > Time.time ? hitColor : color; }
    public void BeginDying()
    {
        // I end the beam immediately and leave my red body visible until my one death delay finishes.
        StopMoving(); laser.Hide(); Target = null;
        hitFlashUntil = 0f; hitbox.enabled = false; body.simulated = false;
        SetColor(dyingColor);
    }
    public void Despawn() { spriteRenderer.enabled = false; gameObject.SetActive(false); }

    public override void CompleteDeath()
    {
        BeginDeath();
    }
}

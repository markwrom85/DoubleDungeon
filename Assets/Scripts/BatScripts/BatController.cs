using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(EnemyDungeonSetup))]
public sealed class BatController : EnemyController
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float batMaxHealth = 15f;
    [SerializeField, Min(0f)] private float deathDuration = 0.6f;
    [Header("Evasive recovery")]
    [SerializeField, Min(0f)] private float initialDelay = 2.5f;
    [SerializeField] private Vector2 recoveryDelay = new Vector2(4f, 6f);
    [SerializeField, Min(0.1f)] private float roamSpeed = 5f;
    [SerializeField, Min(0.05f)] private float directionChangeInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float avoidanceDistance = 3f;
    [Header("Attack preparation")]
    [SerializeField, Range(0.1f, 0.5f)] private float outerRoomFraction = 0.25f;
    [SerializeField, Range(0.05f, 0.4f)] private float arcHeightFraction = 0.22f;
    [SerializeField, Min(0f)] private float chargeDuration = 0.8f;
    [Header("Swoop")]
    [SerializeField, Min(0.1f)] private float swoopSpeed = 11f;
    [SerializeField, Min(0f)] private float contactDamage = 1f;
    [Header("State colors")]
    [SerializeField] private Color roamingColor = new Color(0.55f, 0.3f, 0.85f);
    [SerializeField] private Color chargingColor = new Color(1f, 0.75f, 0.15f);
    [SerializeField] private Color swoopingColor = new Color(1f, 0.2f, 0.45f);
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private Color dyingColor = Color.red;

    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private MeshRenderer bodyRenderer;
    private MaterialPropertyBlock bodyProperties;
    private static readonly int TintId = Shader.PropertyToID("_White");
    private EnemyDungeonSetup dungeonSetup;
    private DungeonManager manager;
    private Vector3 originalScale;
    private Vector2 position, roamDestination, stagePosition;
    private float referenceRoomHeight, recoveryUntil, nextDirectionTime, chargeUntil, hitFlashUntil, deathUntil;
    private float passDistance;
    private bool onLeft, hasRoamDestination, preparingAttack, atEdge, dying, defeatReported;
    private int completedAttacks;
    private Color stateColor;
    private readonly HashSet<PlayerInput> hitPlayers = new HashSet<PlayerInput>();
    private readonly List<Collider2D> playerColliders = new List<Collider2D>(8);

    public BatRoamState RoamState { get; private set; }
    public BatSwoopState SwoopState { get; private set; }
    public EnemyDungeonSide CurrentSide { get; private set; }
    public BatSwoopPath CurrentPass { get; private set; }
    public PlayerInput Target { get; private set; }
    public Vector2 Position => position;
    public Vector2 LockedTargetPosition => CurrentPass != null ? CurrentPass.Target : position;
    public bool IsOnLeft => onLeft;
    public bool IsSwooping => !dying && CurrentState == SwoopState;
    public bool IsPreparingAttack => preparingAttack;
    public int PassIndex { get; private set; }
    public int CompletedAttacks => completedAttacks;
    private float RoomScale => CurrentSide != null && referenceRoomHeight > 0f
        ? CurrentSide.LevelBounds.bounds.size.y / referenceRoomHeight : 1f;
    private float Radius => hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

    protected override void Awake()
    {
        base.Awake();
        MaxHealth = batMaxHealth;
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        bodyRenderer = GetComponentInChildren<MeshRenderer>(true);
        bodyProperties = new MaterialPropertyBlock();
        dungeonSetup = GetComponent<EnemyDungeonSetup>();
        manager = FindAnyObjectByType<DungeonManager>();
        originalScale = transform.localScale;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.useFullKinematicContacts = false;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Keep a solid hurtbox for existing player bullets, without pushing players or walls.
        hitbox.isTrigger = false;
        hitbox.forceSendLayers = 0;
        hitbox.forceReceiveLayers = 0;
        RoamState = new BatRoamState(this);
        SwoopState = new BatSwoopState(this);
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
        position = body.position;
        Health = MaxHealth;
        CurrentSide = null;
        referenceRoomHeight = 0f;
        completedAttacks = 0;
        dying = defeatReported = false;
        hitFlashUntil = 0f;
        hitbox.enabled = body.simulated = bodyRenderer.enabled = true;
        PrepareRoom();
        ChangeState(RoamState);
    }

    protected override void Update()
    {
        if (dying)
        {
            if (Time.time >= deathUntil) CompleteDeath();
            return;
        }
        if (!PrepareRoom())
        {
            if (CurrentState != RoamState) ChangeState(RoamState);
            StopMoving();
            return;
        }
        base.Update();
        if (hitFlashUntil > 0f && Time.time >= hitFlashUntil)
        {
            hitFlashUntil = 0f;
            ApplyColor(stateColor);
        }
    }

    protected override void FixedUpdate()
    {
        if (!dying && IsRoomPairActive()) base.FixedUpdate();
        else StopMoving();
    }

    protected override void OnDisable()
    {
        StopMoving();
        hitPlayers.Clear();
        base.OnDisable();
    }

    public bool PrepareRoom()
    {
        if (CurrentSide == null || CurrentSide.OppositeSide == null)
        {
            CurrentSide = dungeonSetup.GetStartingSide(position);
            if (CurrentSide == null || CurrentSide.OppositeSide == null) return false;
            onLeft = dungeonSetup.GetSide(true) == CurrentSide;
            if (referenceRoomHeight <= 0f) referenceRoomHeight = CurrentSide.LevelBounds.bounds.size.y;
        }
        return IsRoomPairActive();
    }

    public bool IsRoomPairActive()
    {
        if (CurrentSide == null || CurrentSide.OppositeSide == null || CurrentSide.LevelBounds == null
            || CurrentSide.OppositeSide.LevelBounds == null || !CurrentSide.LevelBounds.isActiveAndEnabled
            || !CurrentSide.OppositeSide.LevelBounds.isActiveAndEnabled) return false;
        if (CurrentSide.LevelBounds.bounds.size.x <= 0.001f || CurrentSide.LevelBounds.bounds.size.y <= 0.001f
            || CurrentSide.OppositeSide.LevelBounds.bounds.size.x <= 0.001f
            || CurrentSide.OppositeSide.LevelBounds.bounds.size.y <= 0.001f) return false;
        if (manager == null) return true;
        return CameraBelongsToRoom(onLeft ? manager.leftCinemachineCamera : manager.rightCinemachineCamera, CurrentSide)
            && CameraBelongsToRoom(onLeft ? manager.rightCinemachineCamera : manager.leftCinemachineCamera, CurrentSide.OppositeSide);
    }

    private static bool CameraBelongsToRoom(CinemachineCamera camera, EnemyDungeonSide room)
    {
        Transform parent = room.LevelBounds.transform.parent;
        return camera != null && ((parent != null && camera.transform.IsChildOf(parent))
            || room.LevelBounds.bounds.Contains(camera.transform.position));
    }

    public void BeginRecovery()
    {
        StopMoving();
        preparingAttack = atEdge = hasRoamDestination = false;
        Target = null;
        CurrentPass = null;
        PassIndex = 0;
        float low = Mathf.Max(0f, Mathf.Min(recoveryDelay.x, recoveryDelay.y));
        float high = Mathf.Max(low, Mathf.Max(recoveryDelay.x, recoveryDelay.y));
        recoveryUntil = Time.time + (completedAttacks == 0 ? initialDelay : Random.Range(low, high));
        nextDirectionTime = 0f;
        SetColor(roamingColor);
    }

    public bool UpdateRecovery()
    {
        if (!IsRoomPairActive()) return false;
        if (!preparingAttack && Time.time >= recoveryUntil && HasAnyTarget())
        {
            preparingAttack = true;
            stagePosition = ChooseStagePosition();
            SetColor(chargingColor);
        }
        return atEdge && Time.time >= chargeUntil && HasAnyTarget();
    }

    private bool HasAnyTarget() => CurrentSide.FindTarget(position) != null
        || CurrentSide.OppositeSide.FindTarget(position) != null;

    private Vector2 ChooseStagePosition()
    {
        Bounds bounds = CurrentSide.LevelBounds.bounds;
        PlayerInput target = CurrentSide.FindTarget(position);
        float anchorY = target != null ? target.transform.position.y : bounds.center.y;
        float arc = bounds.size.y * arcHeightFraction;
        float y = anchorY < bounds.center.y ? anchorY + arc : anchorY - arc;
        float x = onLeft ? bounds.min.x + bounds.size.x * outerRoomFraction * 0.5f
            : bounds.max.x - bounds.size.x * outerRoomFraction * 0.5f;
        return ClampInside(bounds, new Vector2(x, y));
    }

    public void MoveDuringRecovery()
    {
        if (!IsRoomPairActive()) return;
        float step = roamSpeed * RoomScale * Time.fixedDeltaTime;
        if (preparingAttack)
        {
            if (atEdge) { StopMoving(); return; }
            MoveTo(Vector2.MoveTowards(position, stagePosition, step));
            if (Vector2.Distance(position, stagePosition) < 0.05f * RoomScale)
            {
                atEdge = true;
                chargeUntil = Time.time + chargeDuration;
            }
            return;
        }
        if (!HasAnyTarget()) { StopMoving(); return; }
        if (!hasRoamDestination || Time.time >= nextDirectionTime || Vector2.Distance(position, roamDestination) < 0.1f * RoomScale)
            ChooseEvasiveDestination();
        MoveTo(Vector2.MoveTowards(position, roamDestination, step));
    }

    private void ChooseEvasiveDestination()
    {
        Bounds bounds = CurrentSide.LevelBounds.bounds;
        PlayerInput threat = CurrentSide.FindTarget(position);
        float bestScore = float.NegativeInfinity;
        for (int index = 0; index < 12; index++)
        {
            Vector2 candidate = ClampInside(bounds, new Vector2(Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)));
            float score = Random.value * 0.5f;
            if (threat != null)
            {
                Vector2 away = position - (Vector2)threat.transform.position;
                float urgency = Mathf.Clamp01(avoidanceDistance * RoomScale / Mathf.Max(0.1f, away.magnitude));
                score += Vector2.Dot((candidate - position).normalized, away.normalized) * urgency * 3f;
                score += Mathf.Min(Vector2.Distance(candidate, threat.transform.position), avoidanceDistance * RoomScale);
            }
            if (score > bestScore) { bestScore = score; roamDestination = candidate; }
        }
        hasRoamDestination = true;
        nextDirectionTime = Time.time + Mathf.Max(0.05f, directionChangeInterval);
    }

    public void BeginSwoop()
    {
        StopMoving();
        preparingAttack = atEdge = false;
        hitPlayers.Clear();
        PassIndex = 0;
        SetColor(swoopingColor);
        BuildPass(false);
    }

    private void BuildPass(bool enteringFromDivider)
    {
        Bounds bounds = CurrentSide.LevelBounds.bounds;
        Target = CurrentSide.FindTarget(position);
        Vector2 aim = Target != null ? (Vector2)Target.transform.position
            : new Vector2(bounds.center.x, position.y < bounds.center.y
                ? position.y + bounds.size.y * arcHeightFraction : position.y - bounds.size.y * arcHeightFraction);
        aim = ClampInside(bounds, aim);
        float endX = enteringFromDivider ? (onLeft ? bounds.min.x + Radius + 0.1f : bounds.max.x - Radius - 0.1f)
            : (onLeft ? bounds.max.x : bounds.min.x);
        float endY = position.y;
        if (enteringFromDivider && Mathf.Abs(aim.y - endY) < bounds.size.y * 0.08f)
            endY = ClampInside(bounds, new Vector2(endX, aim.y < bounds.center.y
                ? aim.y + bounds.size.y * arcHeightFraction : aim.y - bounds.size.y * arcHeightFraction)).y;
        // A target behind the launch point is approached as closely as the room edge permits.
        float lowX = Mathf.Min(position.x, endX), highX = Mathf.Max(position.x, endX);
        float inset = Mathf.Min((highX - lowX) * 0.1f, Radius + 0.05f);
        aim.x = Mathf.Clamp(aim.x, lowX + inset, highX - inset);
        CurrentPass = new BatSwoopPath(position, aim, new Vector2(endX, endY), enteringFromDivider);
        passDistance = 0f;
    }

    public bool AdvanceSwoop(float seconds)
    {
        if (!IsSwooping || !IsRoomPairActive() || CurrentPass == null || seconds <= 0f) return false;
        float timeLeft = seconds;
        for (int pass = 0; pass < 2 && timeLeft > 0f; pass++)
        {
            float speed = Mathf.Max(0.1f, swoopSpeed * RoomScale);
            float travel = Mathf.Min(speed * timeLeft, CurrentPass.Length - passDistance);
            int steps = Mathf.Clamp(Mathf.CeilToInt(travel / Mathf.Max(0.05f, Radius * 0.5f)), 1, 128);
            Vector2 previous = position;
            for (int index = 1; index <= steps; index++)
            {
                Vector2 next = CurrentPass.AtDistance(passDistance + travel * index / steps);
                DamageAlongPath(CurrentSide, previous, next, Radius);
                // The visible portion straddling the portal can hit a player just across the border.
                DamageAlongPath(CurrentSide.OppositeSide, MapAcross(previous), MapAcross(next),
                    Radius * CurrentSide.OppositeSide.LevelBounds.bounds.size.y / CurrentSide.LevelBounds.bounds.size.y);
                previous = next;
            }
            MoveTo(previous);
            passDistance += travel;
            timeLeft -= travel / speed;
            if (passDistance < CurrentPass.Length - 0.0001f) return false;
            if (PassIndex == 1)
            {
                completedAttacks++;
                return true;
            }
            Vector2 destination = MapAcross(CurrentPass.End);
            CurrentSide = CurrentSide.OppositeSide;
            onLeft = !onLeft;
            transform.localScale = originalScale * RoomScale;
            WarpTo(destination);
            PassIndex = 1;
            BuildPass(true); // Select and lock the second room's target only at the crossing.
        }
        return false;
    }

    public Vector2 MapAcross(Vector2 point)
    {
        Bounds from = CurrentSide.LevelBounds.bounds, to = CurrentSide.OppositeSide.LevelBounds.bounds;
        float scale = to.size.y / from.size.y;
        float fromEdge = onLeft ? from.max.x : from.min.x, toEdge = onLeft ? to.min.x : to.max.x;
        return new Vector2(toEdge + (point.x - fromEdge) * scale, to.min.y + (point.y - from.min.y) * scale);
    }

    private void DamageAlongPath(EnemyDungeonSide side, Vector2 from, Vector2 to, float radius)
    {
        if (contactDamage <= 0f) return;
        Vector2 delta = to - from;
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (hitPlayers.Contains(player) || !side.IsValidTarget(player)) continue;
            player.GetComponentsInChildren(false, playerColliders);
            foreach (Collider2D collider in playerColliders)
            {
                if (!collider.enabled || collider.isTrigger) continue;
                float along = delta.sqrMagnitude > 0f
                    ? Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - from, delta) / delta.sqrMagnitude) : 0f;
                Vector2 closest = from + delta * along;
                if ((collider.ClosestPoint(closest) - closest).sqrMagnitude > radius * radius) continue;
                PlayerInfo info = player.GetComponentInChildren<PlayerInfo>(true);
                if (info != null) { hitPlayers.Add(player); info.TakeDamage(contactDamage); }
                break;
            }
        }
    }

    private Vector2 ClampInside(Bounds bounds, Vector2 point)
    {
        float margin = Mathf.Min(Radius + 0.05f, Mathf.Min(bounds.extents.x, bounds.extents.y) * 0.5f);
        return new Vector2(Mathf.Clamp(point.x, bounds.min.x + margin, bounds.max.x - margin),
            Mathf.Clamp(point.y, bounds.min.y + margin, bounds.max.y - margin));
    }

    private void MoveTo(Vector2 next)
    {
        position = next;
        body.MovePosition(next);
    }

    private void WarpTo(Vector2 next)
    {
        position = next;
        body.position = next;
        body.MovePosition(next);
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    public void StopMoving() { if (body != null) body.linearVelocity = Vector2.zero; }
    private void SetColor(Color color)
    {
        stateColor = color;
        ApplyColor(hitFlashUntil > Time.time ? hitColor : color);
    }

    private void ApplyColor(Color color)
    {
        // Tint the shared Unity material without creating a material instance.
        bodyRenderer.GetPropertyBlock(bodyProperties);
        bodyProperties.SetColor(TintId, color);
        bodyRenderer.SetPropertyBlock(bodyProperties);
    }

    public override void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || dying || Health <= 0f || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        Health = Mathf.Max(0f, Health - damage);
        if (Health > 0f)
        {
            hitFlashUntil = Time.time + 0.1f;
            ApplyColor(hitColor);
            return;
        }
        dying = true;
        ChangeState(null);
        StopMoving();
        hitbox.enabled = body.simulated = false;
        hitFlashUntil = 0f;
        SetColor(dyingColor);
        deathUntil = Time.time + deathDuration;
    }

    public override void CompleteDeath()
    {
        if (!dying || defeatReported) return;
        defeatReported = true;
        base.BeginDeath();
        gameObject.SetActive(false);
    }
}

using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyDungeonSetup))]
public sealed class CauldronController : EnemyController
{
    [Header("Reload movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float minimumDistance = 3f;
    [SerializeField, Min(0.1f)] private float maximumDistance = 5f;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Min(0.05f)] private float targetInterval = 0.25f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [Header("Lob attack")]
    [SerializeField] private CauldronProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(0f)] private float initialDelay = 1f;
    [SerializeField, Min(0.1f)] private float reloadDuration = 2.5f;
    [SerializeField, Min(0.05f)] private float windupDuration = 0.5f;
    [SerializeField, Min(0.05f)] private float flightDuration = 1.1f;
    [SerializeField, Min(0.1f)] private float arcHeight = 3f;
    [SerializeField, Min(0f)] private float aimScatter = 0.4f;
    [SerializeField, Min(0.1f)] private float attackRadius = 1.25f;
    [SerializeField, Min(0f)] private float attackDamage = 1f;
    [Header("Final slam")]
    [SerializeField] private CauldronAreaEffect areaPrefab;
    [SerializeField, Min(0.05f)] private float launchDuration = 0.65f;
    [SerializeField, Min(0.1f)] private float launchHeight = 10f;
    [SerializeField, Min(0f)] private float slamWarningDuration = 0.6f;
    [SerializeField, Min(0.05f)] private float fallDuration = 0.5f;
    [SerializeField, Min(0.1f)] private float slamRadius = 2.75f;
    [SerializeField, Min(0f)] private float slamDamage = 2f;
    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform lid;
    [SerializeField, Min(0f)] private float lidLift = 0.25f;
    [Header("State colors")]
    [SerializeField] private Color reloadColor = new Color(0.65f, 1f, 0.65f);
    [SerializeField] private Color attackColor = Color.yellow;
    [SerializeField] private Color launchColor = Color.red;
    [SerializeField] private Color slamColor = Color.cyan;
    [SerializeField] private Color impactColor = new Color(0.7f, 0.2f, 1f);
    [SerializeField] private Color hitColor = Color.white;

    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private EnemyDungeonSetup dungeonSetup;
    private DungeonManager manager;
    private ContactFilter2D obstacleFilter;
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[32];
    private Vector3 visualOrigin, visualScale, lidOrigin, landingPosition;
    private EnemyDungeonSide landingSide;
    private CauldronAreaEffect slamArea;
    private SpriteRenderer[] spriteRenderers;
    private Color stateColor;
    private float hitFlashUntil;
    private float nextTargetTime, readyAt, phaseStarted, riseHeight, fallHeight, landedAt, impactDuration;
    private bool onLeft, hasFired, dying, hasLanding, landed, defeatReported;

    public EnemyDungeonSide CurrentSide { get; private set; }
    public PlayerInput Target { get; private set; }
    public Vector2 Position => body.position;
    public bool HasTarget => CurrentSide != null && CurrentSide.IsValidTarget(Target);
    public bool IsReloading => !dying && CurrentState == ReloadState;
    public float AttackRadius => attackRadius;
    public float SlamRadius => Mathf.Max(attackRadius + 0.1f, slamRadius);
    public CauldronReloadState ReloadState { get; private set; }
    public CauldronAttackState AttackState { get; private set; }
    public CauldronLaunchState LaunchState { get; private set; }
    public CauldronSlamState SlamState { get; private set; }
    public CauldronDespawnedState DespawnedState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        MaxHealth = Health;
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        dungeonSetup = GetComponent<EnemyDungeonSetup>();
        foreach (DungeonManager candidate in FindObjectsByType<DungeonManager>(FindObjectsInactive.Include))
            if (candidate.gameObject.scene == gameObject.scene) { manager = candidate; break; }
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        hitbox.isTrigger = false; // Player bullets require a solid enemy hurtbox.
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;
        spriteRenderers = (visualRoot != null ? visualRoot : transform).GetComponentsInChildren<SpriteRenderer>(true);
        if (visualRoot != null) { visualOrigin = visualRoot.localPosition; visualScale = visualRoot.localScale; }
        if (lid != null) lidOrigin = lid.localPosition;
        ReloadState = new CauldronReloadState(this);
        AttackState = new CauldronAttackState(this);
        LaunchState = new CauldronLaunchState(this);
        SlamState = new CauldronSlamState(this);
        DespawnedState = new CauldronDespawnedState(this);
    }

    private void OnEnable()
    {
        Health = MaxHealth;
        CurrentSide = null;
        Target = null;
        nextTargetTime = 0f;
        hitFlashUntil = 0f;
        hasFired = dying = hasLanding = landed = defeatReported = false;
        hitbox.enabled = body.simulated = true;
        SetVisualHeight(0f);
        if (visualRoot != null) visualRoot.localScale = visualScale;
        SetLid(0f);
        ChangeState(ReloadState);
    }

    protected override void Update()
    {
        base.Update();
        if (hitFlashUntil > 0f && Time.time >= hitFlashUntil)
        {
            hitFlashUntil = 0f;
            ApplyColor(stateColor);
        }
    }

    protected override void FixedUpdate()
    {
        if (!dying && IsRoomActive()) base.FixedUpdate();
        else StopMoving();
    }

    protected override void OnDisable()
    {
        StopMoving();
        if (slamArea != null && !slamArea.HasDetonated) slamArea.Cancel();
        slamArea = null;
        base.OnDisable();
    }

    public bool PrepareRoom()
    {
        if (CurrentSide == null || CurrentSide.OppositeSide == null)
        {
            CurrentSide = dungeonSetup.GetStartingSide(body.position);
            if (CurrentSide == null || CurrentSide.OppositeSide == null) return false;
            onLeft = dungeonSetup.GetSide(true) == CurrentSide;
        }
        return IsRoomActive();
    }

    private bool IsRoomActive()
    {
        if (CurrentSide == null || CurrentSide.OppositeSide == null
            || CurrentSide.LevelBounds == null || CurrentSide.OppositeSide.LevelBounds == null
            || !CurrentSide.LevelBounds.isActiveAndEnabled || !CurrentSide.OppositeSide.LevelBounds.isActiveAndEnabled)
            return false;
        if (manager == null) return true;
        return CameraBelongsToRoom(onLeft ? manager.leftCinemachineCamera : manager.rightCinemachineCamera, CurrentSide)
            && CameraBelongsToRoom(onLeft ? manager.rightCinemachineCamera : manager.leftCinemachineCamera, CurrentSide.OppositeSide);
    }

    private static bool CameraBelongsToRoom(CinemachineCamera camera, EnemyDungeonSide side)
    {
        Transform room = side.LevelBounds.transform.parent;
        return camera != null && ((room != null && camera.transform.IsChildOf(room))
            || side.LevelBounds.bounds.Contains(camera.transform.position));
    }

    public void RefreshTarget()
    {
        if (!HasTarget) Target = null;
        if (Time.time < nextTargetTime) return;
        nextTargetTime = Time.time + targetInterval;
        if (CurrentSide != null) Target = CurrentSide.FindTarget(body.position);
    }

    public void BeginReload()
    {
        SetColor(reloadColor);
        StopMoving();
        SetLid(0f);
        readyAt = Time.time + (hasFired ? reloadDuration : initialDelay);
    }

    public bool CanAttack => HasTarget && Time.time >= readyAt
        && ((Vector2)Target.transform.position - body.position).sqrMagnitude <= attackRange * attackRange;

    public void MoveDuringReload()
    {
        if (!HasTarget) { StopMoving(); return; }
        Vector2 delta = (Vector2)Target.transform.position - body.position;
        float distance = delta.magnitude;
        float near = Mathf.Max(0.1f, minimumDistance);
        float far = Mathf.Max(near, maximumDistance);
        float gap = distance < near ? near - distance : distance > far ? distance - far : 0f;
        if (gap <= 0.02f) { StopMoving(); return; }
        Vector2 direction = distance > 0.001f ? delta / distance : Vector2.right;
        if (distance < near) direction = -direction;
        float speed = Mathf.Min(moveSpeed, gap / Time.fixedDeltaTime);
        float step = speed * Time.fixedDeltaTime;
        Vector2 best = Vector2.zero;
        float bestScore = float.NegativeInfinity;
        // Try small turns around solid obstacles while preserving the desired approach/retreat.
        for (int index = -4; index <= 4; index++)
        {
            Vector2 candidate = Quaternion.Euler(0f, 0f, index * 22.5f) * direction;
            if (!CurrentSide.Contains(body.position + candidate * (step + 0.05f), BodyRadius + 0.05f)) continue;
            int count = Physics2D.CircleCast(body.position, BodyRadius, candidate, obstacleFilter, obstacleHits, step + 0.1f);
            bool blocked = false;
            for (int hit = 0; hit < count; hit++)
                if (obstacleHits[hit].collider != hitbox && !obstacleHits[hit].collider.transform.IsChildOf(transform))
                { blocked = true; break; }
            if (blocked) continue;
            float score = Vector2.Dot(candidate, direction);
            if (score <= bestScore) continue;
            bestScore = score;
            best = candidate;
        }
        body.linearVelocity = best * speed;
    }

    private float BodyRadius => hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
    public void StopMoving() { if (body != null) body.linearVelocity = Vector2.zero; }

    public void BeginAttack() { SetColor(attackColor); StopMoving(); phaseStarted = Time.time; }

    public bool UpdateAttack()
    {
        float progress = Mathf.Clamp01((Time.time - phaseStarted) / Mathf.Max(0.05f, windupDuration));
        SetLid(progress);
        if (progress < 1f) return false;
        if (projectilePrefab != null)
        {
            Vector2 aim = (Vector2)Target.transform.position + Random.insideUnitCircle * aimScatter;
            Bounds bounds = CurrentSide.LevelBounds.bounds;
            float margin = Mathf.Min(attackRadius, Mathf.Min(bounds.extents.x, bounds.extents.y) * 0.9f);
            aim.x = Mathf.Clamp(aim.x, bounds.min.x + margin, bounds.max.x - margin);
            aim.y = Mathf.Clamp(aim.y, bounds.min.y + margin, bounds.max.y - margin);
            CauldronProjectile shot = Instantiate(projectilePrefab, muzzle != null ? muzzle.position : transform.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(shot.gameObject, gameObject.scene);
            shot.Launch(new Vector3(aim.x, aim.y, transform.position.z), CurrentSide,
                flightDuration, arcHeight, attackRadius, attackDamage);
        }
        else Debug.LogError("Cauldron needs its projectile prefab.", this);
        hasFired = true;
        return true;
    }

    private void SetLid(float progress)
    {
        if (lid != null) lid.localPosition = lidOrigin + Vector3.up * (lidLift * progress);
    }

    public override void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || !IsAlive || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        base.TakeDamage(damage);
        if (IsAlive)
        {
            hitFlashUntil = Time.time + 0.1f;
            ApplyColor(hitColor);
        }
    }

    private void SetColor(Color color)
    {
        stateColor = color;
        ApplyColor(hitFlashUntil > Time.time ? hitColor : color);
    }

    private void ApplyColor(Color color)
    {
        foreach (SpriteRenderer sprite in spriteRenderers)
            if (sprite != null) sprite.color = color;
    }

    protected override void BeginDeath()
    {
        if (dying) return;
        dying = true;
        hitFlashUntil = 0f;
        StopMoving();
        PrepareRoom();
        Vector3 deathPosition = new Vector3(body.position.x, body.position.y, transform.position.z);
        landingSide = CurrentSide != null ? CurrentSide.OppositeSide : null;
        hasLanding = dungeonSetup.TryGetOppositePosition(CurrentSide, deathPosition, out landingPosition);
        Target = null;
        hitbox.enabled = body.simulated = false;
        SetLid(0f);
        ChangeState(LaunchState);
    }

    public void BeginLaunch()
    {
        SetColor(launchColor);
        phaseStarted = Time.time;
        riseHeight = GetOffscreenHeight(CurrentSide, body.position.y);
    }

    public bool UpdateLaunch()
    {
        float progress = Mathf.Clamp01((Time.time - phaseStarted) / Mathf.Max(0.05f, launchDuration));
        SetVisualHeight(riseHeight * progress * progress);
        return progress >= 1f;
    }

    public bool BeginSlam()
    {
        if (!hasLanding || landingSide == null || areaPrefab == null)
        {
            Debug.LogWarning("Cauldron cannot slam without an opposite room and area effect prefab; finishing its death.", this);
            return false;
        }
        SetColor(slamColor);
        body.position = landingPosition;
        transform.position = landingPosition;
        CurrentSide = landingSide;
        fallHeight = GetOffscreenHeight(CurrentSide, landingPosition.y);
        SetVisualHeight(fallHeight);
        phaseStarted = Time.time;
        landed = false;
        slamArea = Instantiate(areaPrefab, landingPosition, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(slamArea.gameObject, gameObject.scene);
        slamArea.ShowWarning(CurrentSide, SlamRadius, slamDamage, slamWarningDuration + fallDuration);
        return true;
    }

    public bool UpdateSlam()
    {
        if (landed) return Time.time >= landedAt + impactDuration;
        float elapsed = Time.time - phaseStarted - slamWarningDuration;
        if (elapsed < 0f) return false;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, fallDuration));
        SetVisualHeight(fallHeight * (1f - progress * progress));
        if (progress < 1f) return false;
        landed = true;
        SetColor(impactColor);
        landedAt = Time.time;
        impactDuration = slamArea != null ? slamArea.PulseDuration : 0.35f;
        slamArea?.Detonate();
        return false;
    }

    private float GetOffscreenHeight(EnemyDungeonSide side, float groundY)
    {
        return side != null && side.LevelBounds != null
            ? Mathf.Max(launchHeight, side.LevelBounds.bounds.max.y - groundY + 2f) : launchHeight;
    }

    private void SetVisualHeight(float height)
    {
        if (visualRoot != null)
        {
            // Convert world-up to local space so scaled prefabs still leave the room completely.
            visualRoot.localPosition = visualOrigin + transform.InverseTransformVector(Vector3.up * height);
        }
    }

    public override void CompleteDeath()
    {
        if (!dying || defeatReported) return;
        defeatReported = true;
        base.BeginDeath();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minimumDistance);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(minimumDistance, maximumDistance));
    }

    private void OnValidate()
    {
        maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
        attackRange = Mathf.Max(maximumDistance, attackRange);
        slamRadius = Mathf.Max(attackRadius + 0.1f, slamRadius);
    }
}

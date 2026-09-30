using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
// I store the zombie's health and targeting data, then let its current state control its behavior.
public class ZombieController : MonoBehaviour, IDamageable
{
    // I use the assigned starting side when one is provided.
    [SerializeField] private EnemyDungeonSide startingSide;
    [Header("Stats")]
    [SerializeField, Min(1f)] private float maxHealth = 15f;
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(0.1f)] private float attackRange = 1f;
    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float targetInterval = 0.3f;
    [SerializeField, Min(0f)] private float attackWindup = 0.4f;
    [SerializeField, Min(0.05f)] private float attackRecovery = 0.6f;
    // I time the visible death, hidden transfer, and visible wait before movement resumes.
    [SerializeField, Min(0.05f)] private float deathDuration = 0.7f;
    [SerializeField, Min(0f)] private float transferDuration = 1f;
    [SerializeField, Min(0.05f)] private float resurrectionDuration = 0.8f;

    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private EnemyDungeonSetup dungeonSetup;
    private SpriteRenderer[] spriteRenderers;
    private bool[] rendererVisibility;
    private Vector2 deathPosition;
    private float nextTargetTime;
    private readonly EnemyStateMachine stateMachine = new EnemyStateMachine();
    public ZombieChaseState ChaseState { get; private set; }
    public ZombieAttackState AttackState { get; private set; }
    public ZombieDyingState DyingState { get; private set; }
    public ZombieResurrectingState ResurrectingState { get; private set; }
    public ZombieDespawnedState DespawnedState { get; private set; }
    public EnemyState CurrentState => stateMachine.CurrentState;
    public EnemyDungeonSide CurrentSide { get; private set; }
    public PlayerInput Target { get; private set; }
    public float Health { get; private set; }
    public bool HasResurrected { get; private set; }
    public float AttackWindup => attackWindup;
    public float AttackRecovery => attackRecovery;
    public float DeathDuration => deathDuration;
    public float TransferDuration => transferDuration;
    public float ResurrectionDuration => resurrectionDuration;
    public bool HasTarget => CurrentSide != null && CurrentSide.IsValidTarget(Target);

    // I notify listeners when an attack reaches an eligible player in range.
    public event System.Action<PlayerInput> AttackLanded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        dungeonSetup = GetComponent<EnemyDungeonSetup>();
        // I cache the sprite renderers so I can hide the zombie while its state machine keeps running.
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        rendererVisibility = new bool[spriteRenderers.Length];
        for (int index = 0; index < spriteRenderers.Length; index++)
            rendererVisibility[index] = spriteRenderers[index].enabled;
        // I create the states once and reuse them on each transition.
        ChaseState = new ZombieChaseState(this);
        AttackState = new ZombieAttackState(this);
        DyingState = new ZombieDyingState(this);
        ResurrectingState = new ZombieResurrectingState(this);
        DespawnedState = new ZombieDespawnedState(this);
    }

    private void OnEnable()
    {
        // I reset health, side, and resurrection status whenever the zombie is enabled.
        Health = maxHealth;
        HasResurrected = false;
        CurrentSide = startingSide != null ? startingSide
            : dungeonSetup != null ? dungeonSetup.GetStartingSide(transform.position) : null;
        Target = null;
        // I stagger the first target search so multiple zombies spread their checks across frames.
        nextTargetTime = Time.time + Random.Range(0f, targetInterval);
        SetVisible(true);
        SetCollision(true);
        ChangeState(ChaseState);
    }

    private void Update() { stateMachine.Tick(); }
    private void FixedUpdate() { stateMachine.FixedTick(); }
    private void OnDisable()
    {
        StopMoving();
        stateMachine.ChangeState(null);
    }

    public void ChangeState(EnemyState state) { stateMachine.ChangeState(state); }

    public void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return;
        // I accept damage only while chasing or attacking.
        if (CurrentState != ChaseState && CurrentState != AttackState) return;
        Health = Mathf.Max(0f, Health - damage);
        if (Health <= 0f)
        {
            // I save the death position so resurrection keeps the same offset on the opposite side.
            deathPosition = body.position;
            ChangeState(DyingState);
        }
    }

    public void RefreshTarget()
    {
        // I clear invalid targets immediately and wait between new target searches.
        if (!HasTarget) Target = null;
        if (Time.time < nextTargetTime) return;
        nextTargetTime = Time.time + Mathf.Max(0.05f, targetInterval);
        Target = CurrentSide != null ? CurrentSide.FindTarget(body.position) : null;
    }

    public bool IsTargetInRange()
    {
        return HasTarget && ((Vector2)Target.transform.position - body.position).sqrMagnitude <= attackRange * attackRange;
    }

    public void MoveTowardsTarget()
    {
        // I move directly toward the target while keeping the next step inside this dungeon area.
        if (!HasTarget) { StopMoving(); return; }
        Vector2 direction = ((Vector2)Target.transform.position - body.position).normalized;
        Vector2 step = direction * moveSpeed * Time.fixedDeltaTime;
        body.linearVelocity = CurrentSide.Contains(body.position + step, hitbox.radius)
            ? direction * moveSpeed : Vector2.zero;
    }

    public void StopMoving() { if (body != null) body.linearVelocity = Vector2.zero; }
    public void SetCollision(bool enabled)
    {
        // I toggle the hitbox and physics simulation together.
        hitbox.enabled = enabled;
        body.simulated = enabled;
    }

    public void CompleteAttack()
    {
        // I check the target again at the strike moment so moving out of range avoids the hit.
        if (CurrentState == AttackState && IsTargetInRange()) AttackLanded?.Invoke(Target);
    }

    public void SetVisible(bool visible)
    {
        // I restore only the sprite renderers that were enabled when the zombie was created.
        for (int index = 0; index < spriteRenderers.Length; index++)
            if (spriteRenderers[index] != null)
                spriteRenderers[index].enabled = visible && rendererVisibility[index];
    }

    public bool TryResurrect()
    {
        EnemyDungeonSide destination = CurrentSide != null ? CurrentSide.OppositeSide : null;
        if (destination == null) return false;
        // I add the distance between dungeon centers to the saved death position.
        Vector2 position = deathPosition + (Vector2)(destination.transform.position - CurrentSide.transform.position);
        StopMoving();
        // I move the same zombie by updating its physics and transform positions.
        body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        CurrentSide = destination;
        Target = null;
        Health = maxHealth;
        // I mark this life as resurrected so its next death is final.
        HasResurrected = true;
        nextTargetTime = Time.time;
        return true;
    }
}

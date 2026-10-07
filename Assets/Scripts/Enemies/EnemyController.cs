using UnityEngine;

public abstract class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField, Min(1f)] private float maxHealth = 10f;

    public float Health { get; protected set; }
    public float MaxHealth {get; protected set; }
    public bool IsAlive => Health > 0f;

    public event System.Action<EnemyController> Defeated;

    protected readonly EnemyStateMachine stateMachine = new();
    public EnemyState CurrentState => stateMachine.CurrentState;
    public string CurrentStateName => CurrentState?.GetType().Name ?? "None";

    protected virtual void Awake()
    {
        combatManager = FindAnyObjectByType<CombatManager>();
        Health = maxHealth;
        combatManager?.RegisterEnemy(this);
    }

    public void ChangeState(EnemyState state)
    {
        stateMachine.ChangeState(state);
    }

    protected virtual void Update()
    {
        stateMachine.Tick();
    }

    protected virtual void FixedUpdate()
    {
        stateMachine.FixedTick();
    }

    protected virtual void OnDisable()
    {
        stateMachine.ChangeState(null);
    }

    public virtual void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || !IsAlive || damage <= 0f)
            return;

        Health = Mathf.Max(0f, Health - damage);

        if (Health <= 0f)
            BeginDeath();
    }

    public void RegisterEnemy()
    {
        combatManager?.RegisterEnemy(this);
        Debug.Log($"Registered enemy: {name}. Total active enemies: {combatManager?.ActiveEnemies.Count}");
    }

    protected virtual void BeginDeath()
    {
        combatManager?.OnEnemyDefeated(this);
        Defeated?.Invoke(this);
    }

    public abstract void CompleteDeath();
}

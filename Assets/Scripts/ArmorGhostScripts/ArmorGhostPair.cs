using UnityEngine;

[RequireComponent(typeof(EnemyDungeonSetup))]
// I link the two bodies to one health pool and end both lives when the vulnerable body dies.
public class ArmorGhostPair : MonoBehaviour
{
    [SerializeField] private ArmorGhostController firstBody, secondBody;
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0.05f)] private float deathDuration = 1f;

    private EnemyDungeonSetup dungeonSetup;
    private float despawnTime, nextRoomCheck;
    private bool hasStarted, waitingForRooms;
    public float Health { get; private set; }
    public bool IsDying { get; private set; }
    public ArmorGhostController AttackingBody { get; private set; }
    public ArmorGhostController VulnerableBody { get; private set; }

    private void Awake() { dungeonSetup = GetComponent<EnemyDungeonSetup>(); }
    private void Start()
    {
        hasStarted = true;
        PreparePair();
    }

    private void OnEnable()
    {
        if (hasStarted) PreparePair();
    }

    private void PreparePair()
    {
        if (firstBody == null || secondBody == null || firstBody == secondBody)
        {
            Debug.LogError("ArmorGhostPair needs two different body references.", this);
            gameObject.SetActive(false);
            return;
        }
        // I hide the parked bodies until their actual room pair is available.
        firstBody.WaitForRoom();
        secondBody.WaitForRoom();
        AttackingBody = null;
        VulnerableBody = null;
        Health = maxHealth;
        IsDying = false;
        waitingForRooms = true;
        nextRoomCheck = Time.time;
        InitializePair();
    }

    private void InitializePair()
    {
        EnemyDungeonSide source = dungeonSetup.GetStartingSide(transform.position);
        if (source == null || source.OppositeSide == null
            || !dungeonSetup.TryGetOppositePosition(source, transform.position, out Vector3 partnerPosition)) return;
        EnemyDungeonSide destination = source.OppositeSide;

        waitingForRooms = false;
        Health = maxHealth;
        IsDying = false;
        bool firstAttacks = Random.value < 0.5f;
        AttackingBody = firstAttacks ? firstBody : secondBody;
        VulnerableBody = firstAttacks ? secondBody : firstBody;
        firstBody.Initialize(this, source, transform.position, firstAttacks);
        secondBody.Initialize(this, destination, partnerPosition, !firstAttacks);
    }

    public void TakeDamage(ArmorGhostController source, float damage)
    {
        // I reject armored hits, invalid amounts, and additional damage after death starts.
        if (!isActiveAndEnabled || waitingForRooms || IsDying || source != VulnerableBody
            || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return;
        Health = Mathf.Max(0f, Health - damage);
        if (Health > 0f) { VulnerableBody.FlashOnHit(); return; }

        IsDying = true;
        despawnTime = Time.time + deathDuration;
        firstBody.ChangeState(firstBody.DyingState);
        secondBody.ChangeState(secondBody.DyingState);
    }

    private void Update()
    {
        if (waitingForRooms)
        {
            // I check for the destination pair at intervals while later rooms wait to be entered.
            if (Time.time >= nextRoomCheck)
            {
                nextRoomCheck = Time.time + 0.25f;
                InitializePair();
            }
            return;
        }
        if (!IsDying || Time.time < despawnTime) return;
        firstBody.ChangeState(firstBody.DespawnedState);
        secondBody.ChangeState(secondBody.DespawnedState);
        // I disable the pair after its visible death delay without clearing its fired bullets.
        gameObject.SetActive(false);
    }
}

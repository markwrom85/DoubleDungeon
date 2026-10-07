using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBullet : MonoBehaviour
{
    [Min(0.1f)] public float speed = 8f;
    [Min(0.1f)] public float lifetime = 5f;

    private Rigidbody2D body;
    private EnemyDungeonSide side;
    private float damage, expiresAt;
    public bool IsFlying { get; private set; }
    internal EnemyBulletPool Pool { get; set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    public void Launch(Vector2 direction, float shotDamage, EnemyDungeonSide shotSide)
    {
        // I reset the shot's damage, side, and lifetime whenever I reuse a bullet.
        damage = Mathf.Max(0f, shotDamage);
        side = shotSide;
        body.position = transform.position;
        body.rotation = transform.eulerAngles.z;
        body.angularVelocity = 0f;
        body.linearVelocity = direction.normalized * Mathf.Max(0.1f, speed);
        expiresAt = Time.time + Mathf.Max(0.1f, lifetime);
        IsFlying = true;
    }

    private void Update()
    {
        // I retire shots when their lifetime ends or they leave their original room area.
        if (IsFlying && (Time.time >= expiresAt || side == null || !side.Contains(body.position)))
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsFlying) return;
        PlayerInput player = other.GetComponentInParent<PlayerInput>();
        if (player != null)
        {
            // I use this player's hierarchy so a shared Players parent cannot select another player.
            if (side == null || !side.IsValidTarget(player)) return;
            PlayerInfo info = player.GetComponentInChildren<PlayerInfo>(true);
            float hitDamage = damage;
            ReturnToPool();
            info?.TakeDamage(hitDamage);
            return;
        }

        // I ignore enemy bodies and trigger areas, then stop when I hit a solid obstacle.
        if (other.isTrigger || other.GetComponentInParent<IDamageable>() != null) return;
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        // I consume the shot before disabling it so overlapping callbacks cannot return it twice.
        if (!IsFlying) return;
        IsFlying = false;
        side = null;
        damage = 0f;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        gameObject.SetActive(false);
        if (Pool != null) Pool.Store(this);
        else Destroy(gameObject);
    }

    private void OnDisable() { ReturnToPool(); }
}

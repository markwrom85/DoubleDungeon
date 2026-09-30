using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Min(0)] public float speed = 12;
    [Min(0.1f)] public float lifetime = 2;
    private Transform owner;
    private Rigidbody2D body;
    private float expiresAt;
    private float damage;
    public bool IsFlying { get; private set; }
    internal BulletPool Pool { get; set; }

    public void Launch(Vector2 direction, Transform shooter, float shotDamage = 0f)
    {
        owner = shooter;
        // I reset the damage on each launch so reused bullets receive the current shot's damage.
        damage = Mathf.Max(0f, shotDamage);
        if (body == null) body = GetComponent<Rigidbody2D>();
        body.position = transform.position;
        body.rotation = transform.eulerAngles.z;
        body.angularVelocity = 0;
        body.linearVelocity = direction.normalized * speed;
        expiresAt = Time.time + Mathf.Max(0.1f, lifetime);
        IsFlying = true;
    }

    private void Update()
    {
        if (IsFlying && Time.time >= expiresAt) ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (!IsFlying) return; // Multiple collision callbacks cannot return it twice.
        IsFlying = false;
        owner = null;
        expiresAt = 0;
        if (body != null) { body.linearVelocity = Vector2.zero; body.angularVelocity = 0; }
        gameObject.SetActive(false);
        if (Pool != null) Pool.Store(this);
        else Destroy(gameObject);
    }

    private void OnDisable() { ReturnToPool(); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsFlying) return;
        if (other.isTrigger || other.GetComponentInParent<Bullet>() != null) return;
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) return;
        // I look up the damageable target on this collider or its parent.
        IDamageable target = other.GetComponentInParent<IDamageable>();
        float hitDamage = damage;
        ReturnToPool(); // I consume the shot before applying damage so it cannot hit twice.
        target?.TakeDamage(hitDamage);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        ReturnToPool(); // Return to pool when leaving the trigger area, e.g., for bullets that should not persist.
    }
}

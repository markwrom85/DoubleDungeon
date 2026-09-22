using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Min(0)] public float speed = 12;
    [Min(0.1f)] public float lifetime = 2;
    private Transform owner;
    private Rigidbody2D body;
    private float expiresAt;
    public bool IsFlying { get; private set; }
    internal BulletPool Pool { get; set; }

    public void Launch(Vector2 direction, Transform shooter)
    {
        owner = shooter;
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
        if (other.isTrigger || other.GetComponentInParent<Bullet>() != null) return;
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner))) return;
        ReturnToPool(); // Damage can be added when targets/enemies exist.
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        ReturnToPool(); // Return to pool when leaving the trigger area, e.g., for bullets that should not persist.
    }
}

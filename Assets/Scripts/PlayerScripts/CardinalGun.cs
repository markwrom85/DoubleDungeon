using UnityEngine;

public class CardinalGun : MonoBehaviour
{
    [SerializeField] private PlayerInfo playerInfo;
    public Bullet bulletPrefab;
    public Transform muzzle;
    public CardinalDirection startingDirection = CardinalDirection.Right;
    public CardinalDirection Facing { get; private set; }
    private float nextShotTime;
    [Header("Bullet pool")]
    [Min(1)] public int initialPoolSize = 24;
    [Min(1)] public int maxPoolSize = 128;
    private BulletPool pool;

    private void Awake()
    {
        Facing = startingDirection;
        ApplyFacing();
        EnsurePool();
    }

    private void EnsurePool()
    {
        if (pool == null && bulletPrefab != null)
            pool = new BulletPool(bulletPrefab, initialPoolSize, maxPoolSize, gameObject.scene);
    }

    public void Tick(Vector2 movement, bool fireHeld)
    {
        Facing = CardinalAim.Step(movement.x, movement.y, Facing, fireHeld);
        ApplyFacing();
        if (!fireHeld || Time.timeScale <= 0 || Time.time < nextShotTime) return;
        if (bulletPrefab == null || muzzle == null) return;
        EnsurePool();
        pool.Fire(muzzle.position, transform.rotation, transform.right, transform.root);
        nextShotTime = Time.time + 1f / Mathf.Max(0.1f, playerInfo.AttackRate);
    }

    private void ApplyFacing()
    {
        // World-space cardinal aim, independent of parent movement or rotation.
        transform.rotation = Quaternion.Euler(0, 0, (int)Facing * 90f);
    }

    private void OnDisable() { pool?.ReturnAll(); }
    private void OnDestroy() { pool?.Dispose(); }
}

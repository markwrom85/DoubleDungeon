using UnityEngine;

public class CardinalGun : MonoBehaviour
{
    [SerializeField] private PlayerInfo playerInfo;
    public Bullet bulletPrefab;
    public Transform muzzle;
    public CardinalDirection startingDirection = CardinalDirection.Right;
    public CardinalDirection Facing { get; private set; }

    private float nextShotTime;
    private bool wasFiring;

    [Header("Bullet pool")]
    [Min(1)] public int initialPoolSize = 24;
    [Min(1)] public int maxPoolSize = 128;

    private BulletPool pool;

    public bool canShoot = true;

    private void Awake()
    {
        Facing = startingDirection;
        ApplyFacing();
        EnsurePool();
    }

    private void EnsurePool()
    {
        if (pool == null && bulletPrefab != null)
            pool = new BulletPool(
                bulletPrefab,
                initialPoolSize,
                maxPoolSize,
                gameObject.scene
            );
    }

    public void Tick(Vector2 movement, bool fireHeld)
    {
        if (!canShoot)
            return;

        if (!fireHeld)
        {
            Facing = CardinalAim.FromMovement(
                movement.x,
                movement.y,
                Facing
            );
        }

        ApplyFacing();

        if (fireHeld && !wasFiring && playerInfo != null)
            playerInfo.SetShootingDirection(GetAnimatorDirection(Facing));

        if (playerInfo != null)
            playerInfo.SetShooting(fireHeld);

        wasFiring = fireHeld;

        if (!fireHeld || Time.timeScale <= 0 || Time.time < nextShotTime)
            return;

        if (bulletPrefab == null || muzzle == null)
            return;

        EnsurePool();

        pool.Fire(
            muzzle.position,
            transform.rotation,
            transform.right,
            transform.root,
            playerInfo.AttackDamage
        );

        nextShotTime = Time.time + 1f / Mathf.Max(0.1f, playerInfo.AttackRate);
    }

    private int GetAnimatorDirection(CardinalDirection direction)
    {
        switch (direction)
        {
            case CardinalDirection.Up:
                return 1;

            case CardinalDirection.Left:
                return 2;

            case CardinalDirection.Down:
                return 0;

            default:
                return 3;
        }
    }

    private void ApplyFacing()
    {
        transform.rotation = Quaternion.Euler(0, 0, (int)Facing * 90f);
    }

    private void OnDisable()
    {
        wasFiring = false;
        pool?.ReturnAll();
    }

    private void OnDestroy()
    {
        pool?.Dispose();
    }

}
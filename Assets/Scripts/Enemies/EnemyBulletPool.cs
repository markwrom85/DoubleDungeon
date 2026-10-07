using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// I share a bounded bullet pool between enemies using the same projectile in the same scene.
public class EnemyBulletPool : MonoBehaviour
{
    private static readonly List<EnemyBulletPool> pools = new List<EnemyBulletPool>();
    private readonly Stack<EnemyBullet> available = new Stack<EnemyBullet>();
    private readonly List<EnemyBullet> bullets = new List<EnemyBullet>();
    private EnemyBullet prefab;
    private int maximum;
    private bool shuttingDown;
    public int Count => bullets.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools() { pools.Clear(); }

    public static EnemyBulletPool GetOrCreate(EnemyBullet projectile, Scene scene, int initialSize, int maxSize)
    {
        if (projectile == null) return null;
        foreach (EnemyBulletPool pool in pools)
            if (pool != null && pool.prefab == projectile && pool.gameObject.scene == scene)
                return pool;

        // I keep the pool outside the enemy hierarchy so fired shots survive their shooter's death.
        var root = new GameObject("Enemy Bullet Pool");
        SceneManager.MoveGameObjectToScene(root, scene);
        EnemyBulletPool created = root.AddComponent<EnemyBulletPool>();
        created.prefab = projectile;
        created.maximum = Mathf.Max(1, maxSize);
        pools.Add(created);
        for (int index = 0; index < Mathf.Clamp(initialSize, 0, created.maximum); index++)
            created.available.Push(created.Create());
        return created;
    }

    private EnemyBullet Create()
    {
        EnemyBullet bullet = Instantiate(prefab, transform);
        bullet.gameObject.SetActive(false);
        bullet.Pool = this;
        bullets.Add(bullet);
        return bullet;
    }

    public EnemyBullet Fire(Vector3 position, Vector2 direction, float damage, EnemyDungeonSide side)
    {
        if (shuttingDown || side == null || direction.sqrMagnitude < 0.0001f) return null;
        EnemyBullet bullet = null;
        while (available.Count > 0 && bullet == null) bullet = available.Pop();
        if (bullet == null)
        {
            bullets.RemoveAll(item => item == null);
            // I skip a shot when the pool is full instead of replacing a projectile still in flight.
            if (bullets.Count >= maximum) return null;
            bullet = Create();
        }
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.gameObject.SetActive(true);
        bullet.Launch(direction, damage, side);
        return bullet;
    }

    internal void Store(EnemyBullet bullet)
    {
        if (!shuttingDown) available.Push(bullet);
    }

    private void OnDestroy()
    {
        shuttingDown = true;
        pools.Remove(this);
        available.Clear();
        bullets.Clear();
    }
}

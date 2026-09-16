using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// One pool per gun. Its world-space root keeps bullets independent of player movement.
public sealed class BulletPool : IDisposable
{
    private readonly Bullet prefab;
    private readonly Transform root;
    private readonly int maximum;
    private readonly Stack<Bullet> available = new Stack<Bullet>();
    private readonly List<Bullet> bullets = new List<Bullet>();
    private bool disposed;
    public int Count => bullets.Count;

    public BulletPool(Bullet prefab, int initialSize, int maxSize, Scene scene)
    {
        this.prefab = prefab;
        maximum = Mathf.Max(1, maxSize);
        root = new GameObject("Bullet Pool").transform;
        SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        for (int i = 0; i < Mathf.Clamp(initialSize, 0, maximum); i++) available.Push(Create());
    }

    private Bullet Create()
    {
        Bullet bullet = UnityEngine.Object.Instantiate(prefab, root);
        bullet.gameObject.SetActive(false);
        bullet.Pool = this;
        bullets.Add(bullet);
        return bullet;
    }

    public Bullet Fire(Vector3 position, Quaternion rotation, Vector2 direction, Transform owner)
    {
        if (disposed) return null;
        Bullet bullet = null;
        while (available.Count > 0 && bullet == null) bullet = available.Pop();
        if (bullet == null)
        {
            bullets.RemoveAll(item => item == null);
            if (bullets.Count >= maximum) return null; // Never recycle a bullet still in flight.
            bullet = Create();
        }
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.gameObject.SetActive(true);
        bullet.Launch(direction, owner);
        return bullet;
    }

    internal void Store(Bullet bullet)
    {
        if (!disposed) available.Push(bullet);
    }

    public void ReturnAll()
    {
        foreach (Bullet bullet in bullets)
            if (bullet != null) bullet.ReturnToPool();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ReturnAll();
        available.Clear();
        bullets.Clear();
        if (root != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(root.gameObject);
            else UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HyperBeamCollision : MonoBehaviour
{
    [SerializeField] private float damageCooldown = 0.75f;

    public float damage;
    private List<IDamageable> enemies = new List<IDamageable>();
    private bool canDamage = true;
    private Coroutine damageCoroutine;


    void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            enemies.Add(target);
        }
        target?.TakeDamage(damage);
    }

    private void Update()
    {
        if (canDamage)
        {
            foreach (IDamageable enemy in enemies)
            {
                enemy.TakeDamage(damage);
            }
            damageCoroutine = StartCoroutine(DamageBuffer());
        }
    }

    private IEnumerator DamageBuffer()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageCooldown);
        canDamage = true;
    }

    void OnDisable()
    {
        StopCoroutine(damageCoroutine);
    }
}

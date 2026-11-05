using UnityEngine;

public class AOEBurst : MonoBehaviour
{
    public float aoeDamage;
    
    void Start()
    {
        aoeDamage = GetComponentInParent<PlayerInfo>().AOEDamage;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        target?.TakeDamage(aoeDamage);
    }
}

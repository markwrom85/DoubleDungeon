using UnityEngine;

public class AOEBurst : MonoBehaviour
{
    public float aoeDamage;
    
    void Start()
    {
        if(GetComponentInParent<PlayerInfo>() != null)
        {
            aoeDamage = GetComponentInParent<PlayerInfo>().AOEDamage;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        target?.TakeDamage(aoeDamage);
    }
}

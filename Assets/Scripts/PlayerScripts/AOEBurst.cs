using UnityEngine;

public class AOEBurst : MonoBehaviour
{
    [SerializeField] private PlayerInfo playerInfo;
    
    void Start()
    {
        playerInfo = GetComponentInParent<PlayerInfo>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        target?.TakeDamage(playerInfo.AOEDamage);
    }
}

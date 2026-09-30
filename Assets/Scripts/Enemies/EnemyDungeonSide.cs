using UnityEngine;
using UnityEngine.InputSystem;

// I track one dungeon area's bounds, opposite side, and eligible player targets.
public class EnemyDungeonSide : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(18f, 10f);
    [SerializeField] private EnemyDungeonSide oppositeSide;

    public EnemyDungeonSide OppositeSide => oppositeSide;

    public void Configure(Vector2 areaSize, EnemyDungeonSide opposite)
    {
        size = areaSize;
        oppositeSide = opposite;
    }

    public bool Contains(Vector2 position, float margin = 0f)
    {
        // I apply a margin to keep the enemy's body inside the rectangular bounds.
        Vector2 offset = position - (Vector2)transform.position;
        return Mathf.Abs(offset.x) < size.x * 0.5f - margin
            && Mathf.Abs(offset.y) < size.y * 0.5f - margin;
    }

    public bool IsValidTarget(PlayerInput player)
    {
        if (player == null || !player.isActiveAndEnabled || !Contains(player.transform.position)) return false;
        // I check the character's active state so enemies ignore teleport placement.
        if (player.TryGetComponent(out EnemyTarget target)) return target.IsTargetable;
        return true;
    }

    public PlayerInput FindTarget(Vector2 position)
    {
        PlayerInput closest = null;
        float distance = float.PositiveInfinity;
        // I check the Input System's player list to find the nearest eligible target.
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (!IsValidTarget(player)) continue;
            // I compare squared distances to avoid calculating square roots.
            float candidate = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (candidate >= distance) continue;
            closest = player;
            distance = candidate;
        }
        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
    }
}

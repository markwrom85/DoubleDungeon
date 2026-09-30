using UnityEngine;
using UnityEngine.InputSystem;

// I track one dungeon area's bounds, opposite side, and eligible player targets.
public class EnemyDungeonSide : MonoBehaviour
{
    private Collider2D levelBounds;
    [SerializeField] private EnemyDungeonSide oppositeSide;

    public EnemyDungeonSide OppositeSide => oppositeSide;

    public void SetBounds(Collider2D bounds)
    {
        levelBounds = bounds;
        transform.position = bounds.bounds.center;
    }

    public void Configure(EnemyDungeonSide opposite)
    {
        oppositeSide = opposite;
    }

    public bool Contains(Vector2 position, float margin = 0f)
    {
        if (levelBounds == null) return false;
        Bounds bounds = levelBounds.bounds;
        return position.x > bounds.min.x + margin && position.x < bounds.max.x - margin
            && position.y > bounds.min.y + margin && position.y < bounds.max.y - margin;
    }

    public Vector2 GetRandomPosition(float edgeMargin = 0.5f)
    {
        if (levelBounds == null) return transform.position;
        Bounds bounds = levelBounds.bounds;
        return new Vector2(
            Random.Range(bounds.min.x + edgeMargin, bounds.max.x - edgeMargin),
            Random.Range(bounds.min.y + edgeMargin, bounds.max.y - edgeMargin));
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
        if (levelBounds != null)
            Gizmos.DrawWireCube(levelBounds.bounds.center, levelBounds.bounds.size);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// I create one pair of dungeon areas for the enemies in each scene.
public class EnemyDungeonSetup : MonoBehaviour
{
    // I use level-bound colliders to define the two enemy areas.
    [SerializeField] private Collider2D leftBounds, rightBounds;
    private static readonly Dictionary<Scene, EnemyDungeonSide> sceneAreas = new Dictionary<Scene, EnemyDungeonSide>();

    // I clear the cached areas when Play mode starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAreas() { sceneAreas.Clear(); }

    public EnemyDungeonSide GetStartingSide(Vector2 position)
    {
        EnemyDungeonSide left = EnsureAreas();
        EnemyDungeonSide other = left.OppositeSide;
        // I choose the starting side by comparing the spawn position with each area's center.
        return (position - (Vector2)left.transform.position).sqrMagnitude
            <= (position - (Vector2)other.transform.position).sqrMagnitude ? left : other;
    }

    public EnemyDungeonSide GetSide(bool leftSide)
    {
        EnemyDungeonSide left = EnsureAreas();
        return leftSide ? left : left.OppositeSide;
    }

    public Vector2 GetSpawnPosition(bool leftSide, float edgeMargin = 0.5f)
    {
        EnemyDungeonSide side = GetSide(leftSide);
        return side.GetRandomPosition(edgeMargin);
    }

    private EnemyDungeonSide EnsureAreas()
    {
        Scene sceneId = gameObject.scene;
        if (sceneAreas.TryGetValue(sceneId, out EnemyDungeonSide existing) && existing != null)
            return existing;

        if (leftBounds == null || rightBounds == null)
        {
            Debug.LogError("EnemyDungeonSetup needs left and right level-bound colliders assigned.", this);
            return null;
        }

        var root = new GameObject("Enemy Dungeon Areas");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        EnemyDungeonSide left = CreateSide(root.transform, "Left Enemy Area", leftBounds);
        EnemyDungeonSide right = CreateSide(root.transform, "Right Enemy Area", rightBounds);
        left.Configure(right);
        right.Configure(left);
        sceneAreas[sceneId] = left;
        return left;
    }

    private static EnemyDungeonSide CreateSide(Transform parent, string name, Collider2D bounds)
    {
        var area = new GameObject(name);
        area.transform.SetParent(parent, false);
        EnemyDungeonSide side = area.AddComponent<EnemyDungeonSide>();
        side.SetBounds(bounds);
        return side;
    }

}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// I create one pair of dungeon areas for the enemies in each scene.
public class EnemyDungeonSetup : MonoBehaviour
{
    // I use these world-space centers and dimensions to define the two dungeon areas.
    [SerializeField] private Vector2 leftCenter = new Vector2(-9f, 0f);
    [SerializeField] private Vector2 rightCenter = new Vector2(9f, 0f);
    [SerializeField] private Vector2 areaSize = new Vector2(18f, 10f);
    private static readonly Dictionary<Scene, EnemyDungeonSide> sceneAreas = new Dictionary<Scene, EnemyDungeonSide>();

    // I clear the cached areas when Play mode starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAreas() { sceneAreas.Clear(); }

    public EnemyDungeonSide GetStartingSide(Vector2 position)
    {
        Scene sceneId = gameObject.scene;
        // I reuse the areas created by the first enemy in this scene.
        if (!sceneAreas.TryGetValue(sceneId, out EnemyDungeonSide left) || left == null)
        {
            var root = new GameObject("Enemy Dungeon Areas");
            // I put the areas under a separate root so they stay in place when enemies move.
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            left = CreateSide(root.transform, "Left Enemy Area", leftCenter);
            EnemyDungeonSide right = CreateSide(root.transform, "Right Enemy Area", rightCenter);
            left.Configure(areaSize, right);
            right.Configure(areaSize, left);
            sceneAreas[sceneId] = left;
        }
        EnemyDungeonSide other = left.OppositeSide;
        // I choose the starting side by comparing the spawn position with each area's center.
        return (position - (Vector2)left.transform.position).sqrMagnitude
            <= (position - (Vector2)other.transform.position).sqrMagnitude ? left : other;
    }

    private static EnemyDungeonSide CreateSide(Transform parent, string name, Vector2 center)
    {
        var area = new GameObject(name);
        area.transform.SetParent(parent, false);
        area.transform.position = center;
        return area.AddComponent<EnemyDungeonSide>();
    }

}

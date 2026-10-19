using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
// I keep each enemy attached to the room pair where its life begins.
public class EnemyDungeonSetup : MonoBehaviour
{
    [SerializeField] private bool automaticallyFindRooms = true;
    [SerializeField] private Collider2D leftBounds, rightBounds;
    [SerializeField] private Camera leftCamera, rightCamera;
    private EnemyDungeonSide boundLeft;
    private static readonly Dictionary<Scene, SceneAreas> sceneAreas = new Dictionary<Scene, SceneAreas>();

    private class SceneAreas
    {
        public readonly List<Collider2D> rooms = new List<Collider2D>();
        public readonly Dictionary<(Collider2D, Collider2D), EnemyDungeonSide> pairs
            = new Dictionary<(Collider2D, Collider2D), EnemyDungeonSide>();
        public readonly Dictionary<Collider2D, EnemyDungeonSide> waitingRooms
            = new Dictionary<Collider2D, EnemyDungeonSide>();
        public DungeonManager manager;
        public Transform root;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAreas()
    {
        sceneAreas.Clear();
        SceneManager.sceneUnloaded -= RemoveScene;
        SceneManager.sceneUnloaded += RemoveScene;
    }

    private static void RemoveScene(Scene scene) { sceneAreas.Remove(scene); }
    private void OnEnable() { boundLeft = null; }

    public void ConfigureRoomPair(Collider2D left, Collider2D right, Camera leftView = null, Camera rightView = null)
    {
        // I let a spawner supply the destination pair before enabling the enemy.
        leftBounds = left;
        rightBounds = right;
        leftCamera = leftView;
        rightCamera = rightView;
        automaticallyFindRooms = false;
        boundLeft = null;
    }

    public EnemyDungeonSide GetStartingSide(Vector2 position)
    {
        SceneAreas areas = GetAreas();
        TryBindPair(areas, position);
        if (boundLeft != null)
        {
            // I require the spawn to be inside one of its rooms instead of choosing an unrelated nearby room.
            if (boundLeft.Contains(position)) return boundLeft;
            if (boundLeft.OppositeSide.Contains(position)) return boundLeft.OppositeSide;
            return null;
        }
        if (!automaticallyFindRooms) return null;
        Collider2D room = FindRoom(areas, position);
        if (room == null) return null;
        // I give a dormant zombie its own room bounds while it waits for an opposite room to become active.
        if (!areas.waitingRooms.TryGetValue(room, out EnemyDungeonSide side) || side == null)
        {
            side = CreateSide(GetRoot(areas), room.transform.parent != null ? room.transform.parent.name : room.name, room);
            areas.waitingRooms[room] = side;
        }
        return side;
    }

    public EnemyDungeonSide GetSide(bool leftSide)
    {
        TryBindPair(GetAreas(), transform.position);
        return boundLeft == null ? null : leftSide ? boundLeft : boundLeft.OppositeSide;
    }

    public Vector2 GetSpawnPosition(bool leftSide, float edgeMargin = 0.5f)
    {
        EnemyDungeonSide side = GetSide(leftSide);
        return side != null ? side.GetRandomPosition(edgeMargin) : (Vector2)transform.position;
    }

    public Camera GetCameraForSide(EnemyDungeonSide side)
    {
        if (side == null || side.LevelBounds == null) return null;
        Camera camera = side.LevelBounds == leftBounds ? leftCamera
            : side.LevelBounds == rightBounds ? rightCamera : null;
        // I use a view only while it still belongs to this enemy's room.
        return camera != null && side.Contains(camera.transform.position) ? camera : null;
    }

    public bool TryGetOppositePosition(EnemyDungeonSide source, Vector3 position, out Vector3 destinationPosition)
    {
        destinationPosition = position;
        EnemyDungeonSide destination = source != null ? source.OppositeSide : null;
        if (destination == null || source.LevelBounds == null || destination.LevelBounds == null) return false;
        Camera sourceCamera = GetCameraForSide(source);
        Camera destinationCamera = GetCameraForSide(destination);
        if (sourceCamera != null && destinationCamera != null)
        {
            Vector3 viewport = sourceCamera.WorldToViewportPoint(position);
            float depth = Mathf.Abs(position.z - destinationCamera.transform.position.z);
            Vector3 mapped = destinationCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
            if (destination.Contains(mapped))
            {
                destinationPosition = new Vector3(mapped.x, mapped.y, position.z);
                return true;
            }
        }
        // I preserve the relative room position even if the display cameras have already moved elsewhere.
        Bounds from = source.LevelBounds.bounds;
        Bounds to = destination.LevelBounds.bounds;
        if (from.size.x <= 0f || from.size.y <= 0f || to.size.x <= 0f || to.size.y <= 0f) return false;
        float x = Mathf.InverseLerp(from.min.x, from.max.x, position.x);
        float y = Mathf.InverseLerp(from.min.y, from.max.y, position.y);
        destinationPosition = new Vector3(Mathf.Lerp(to.min.x, to.max.x, x), Mathf.Lerp(to.min.y, to.max.y, y), position.z);
        return true;
    }

    private SceneAreas GetAreas()
    {
        Scene scene = gameObject.scene;
        if (!sceneAreas.TryGetValue(scene, out SceneAreas areas))
        {
            areas = new SceneAreas();
            // I discover the existing room markers once per scene rather than searching during movement updates.
            foreach (Collider2D collider in FindObjectsByType<Collider2D>(FindObjectsInactive.Include))
                if (collider.gameObject.scene == scene && collider.name == "LevelBounds") areas.rooms.Add(collider);
            foreach (DungeonManager manager in FindObjectsByType<DungeonManager>(FindObjectsInactive.Include))
                if (manager.gameObject.scene == scene) { areas.manager = manager; break; }
            sceneAreas[scene] = areas;
        }
        AddRoom(areas, leftBounds);
        AddRoom(areas, rightBounds);
        return areas;
    }

    private void AddRoom(SceneAreas areas, Collider2D room)
    {
        if (room != null && room.gameObject.scene == gameObject.scene && !areas.rooms.Contains(room)) areas.rooms.Add(room);
    }

    private void TryBindPair(SceneAreas areas, Vector2 position)
    {
        // I leave an established pair unchanged when the manager switches to another pair of rooms.
        if (boundLeft != null) return;
        Collider2D left = null, right = null;
        if (automaticallyFindRooms && areas.manager != null)
        {
            Transform leftView = areas.manager.leftCinemachineCamera != null ? areas.manager.leftCinemachineCamera.transform : null;
            Transform rightView = areas.manager.rightCinemachineCamera != null ? areas.manager.rightCinemachineCamera.transform : null;
            left = FindCameraRoom(areas, leftView);
            right = FindCameraRoom(areas, rightView);
            // I wait for this room to enter the active pair instead of binding an automatic enemy to old Inspector references.
            if (left != null && right != null && left != right && !ValidPair(left, right, position)) return;
        }
        if (!ValidPair(left, right, position)) { left = leftBounds; right = rightBounds; }
        if (!ValidPair(left, right, position)) return;
        // I share areas only when both collider references and their left/right assignment match.
        var key = (left, right);
        if (!areas.pairs.TryGetValue(key, out EnemyDungeonSide side) || side == null)
        {
            var pair = new GameObject(left.name + " + " + right.name);
            SceneManager.MoveGameObjectToScene(pair, gameObject.scene);
            pair.transform.SetParent(GetRoot(areas), false);
            EnemyDungeonSide first = CreateSide(pair.transform, "Left Enemy Area", left);
            EnemyDungeonSide second = CreateSide(pair.transform, "Right Enemy Area", right);
            first.Configure(second);
            second.Configure(first);
            areas.pairs[key] = first;
            side = first;
        }
        boundLeft = side;
    }

    private bool ValidPair(Collider2D left, Collider2D right, Vector2 position)
    {
        return left != null && right != null && left != right
            && left.gameObject.scene == gameObject.scene && right.gameObject.scene == gameObject.scene
            && (Contains(left, position) || Contains(right, position));
    }

    private static Collider2D FindCameraRoom(SceneAreas areas, Transform camera)
    {
        if (camera == null) return null;
        foreach (Collider2D room in areas.rooms)
            if (room != null && room.transform.parent != null && camera.IsChildOf(room.transform.parent)) return room;
        return FindRoom(areas, camera.position);
    }

    private static Collider2D FindRoom(SceneAreas areas, Vector2 position)
    {
        foreach (Collider2D room in areas.rooms)
            if (Contains(room, position)) return room;
        return null;
    }

    private static bool Contains(Collider2D room, Vector2 position)
    {
        if (room == null || !room.enabled || !room.gameObject.activeInHierarchy) return false;
        Bounds bounds = room.bounds;
        return position.x > bounds.min.x && position.x < bounds.max.x
            && position.y > bounds.min.y && position.y < bounds.max.y;
    }

    private Transform GetRoot(SceneAreas areas)
    {
        if (areas.root != null) return areas.root;
        var root = new GameObject("Enemy Dungeon Areas");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        areas.root = root.transform;
        return areas.root;
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

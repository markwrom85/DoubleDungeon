using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// An invisible, camera-attached opening shared by lasers and swooping Bats.
// Camera coordinates fit the opening; routing uses the physical portal transforms.
[RequireComponent(typeof(BoxCollider2D))]
[DefaultExecutionOrder(1100)]
public sealed class Portal : MonoBehaviour
{
    #region Settings and runtime state

    public enum CameraEdge { Left = -1, Right = 1 }

    [SerializeField] private Camera attachedCamera;
    [SerializeField] private CameraEdge cameraEdge = CameraEdge.Right;
    [SerializeField] private Portal linkedPortal;
    [SerializeField] private bool followCamera = true;
    [SerializeField] private float gameplayZ;
    [SerializeField, Min(0.01f)] private float triggerThickness = 0.1f;

    private BoxCollider2D opening;

    // All portals share these registries so enemies can find a connection and track their last exit.
    private static readonly List<Portal> portals = new List<Portal>();
    private static readonly HashSet<BatController> bats = new HashSet<BatController>();
    private static readonly Dictionary<BatController, Portal> lastExit = new Dictionary<BatController, Portal>();

    // Each entrance owns the visual copies it displays through its linked exit.
    private readonly Dictionary<BatController, VisualCopy> copies = new Dictionary<BatController, VisualCopy>();

    #endregion

    #region Portal geometry

    public Portal LinkedPortal => linkedPortal;

    // Cache the trigger collider that defines the opening.
    public BoxCollider2D Opening => opening != null ? opening : opening = GetComponent<BoxCollider2D>();

    public Vector2 Center => transform.TransformPoint(Opening.offset);
    public Vector2 Normal => transform.right * (float)cameraEdge;
    public Vector2 Up => transform.up;
    public float Height => Opening.size.y * transform.TransformVector(Vector3.up).magnitude;

    // Preserve apparent size when the two cameras show different amounts of the world.
    public float ExitScale => linkedPortal.Height / Height;

    private float Thickness => Opening.size.x * transform.TransformVector(Vector3.right).magnitude;
    private float PixelSize => attachedCamera != null && attachedCamera.pixelHeight > 0
        ? Height / attachedCamera.pixelHeight
        : 0.002f;

    #endregion

    #region Data passed between portals and enemies

    // Describes the Bat's position, direction, and size after a successful transfer.
    public struct Transit
    {
        public Portal Exit;
        public Vector2 Position, Direction;
        public float Scale;
    }

    // A section of beam or Bat movement, including the room and portal-side contact limits.
    public struct Segment
    {
        public Vector2 Start, End;
        public float Width;
        public EnemyDungeonSide Room;
        internal Portal Gate;
        internal bool ClipHeight;

        // Reject contacts on the hidden side of the opening, and outside its height when requested.
        public bool AllowsContact(Vector2 point) => Gate == null || Gate.ContainsVisible(point, ClipHeight);
    }

    // Holds the straight beam section in each room.
    public struct BeamPath
    {
        public Segment Source, Destination;
        public bool HasDestination;
        public float Z;
    }

    // Keeps a render-only Bat copy and its reusable color/property data.
    private sealed class VisualCopy
    {
        public MeshRenderer Source, Renderer;
        public readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
    }

    #endregion

    #region Unity lifecycle

    // Clear shared references when gameplay starts, including when domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        portals.Clear();
        bats.Clear();
        lastExit.Clear();
    }

    // Make the opening non-blocking and fit it before enemies first use it.
    private void Awake()
    {
        Opening.isTrigger = true;
        FitToCamera();
    }

    // Make this portal available to enemies looking for their room's connection.
    private void OnEnable()
    {
        if (!portals.Contains(this))
            portals.Add(this);
    }

    // Stop new routing through this portal and remove the visual copies it owns.
    private void OnDisable()
    {
        portals.Remove(this);
        ClearCopies();
    }

    // Release any remaining visual copies when the portal object is destroyed.
    private void OnDestroy()
    {
        ClearCopies();
    }

    #endregion

    #region Camera attachment and connection validation

    // Place the opening along this camera's assigned edge and match its visible height.
    public void FitToCamera()
    {
        if (!followCamera || attachedCamera == null || !attachedCamera.orthographic)
            return;

        float depth = Mathf.Abs(gameplayZ - attachedCamera.transform.position.z);
        float edge = cameraEdge == CameraEdge.Right ? 1f : 0f;
        Vector3 bottom = attachedCamera.ViewportToWorldPoint(new Vector3(edge, 0f, depth));
        Vector3 top = attachedCamera.ViewportToWorldPoint(new Vector3(edge, 1f, depth));
        Vector3 center = (bottom + top) * 0.5f;
        center.z = gameplayZ;

        transform.SetPositionAndRotation(center, attachedCamera.transform.rotation);
        transform.localScale = Vector3.one;

        Vector3 scale = transform.lossyScale;
        Opening.size = new Vector2(
            triggerThickness / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            Vector3.Distance(bottom, top) / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
        Opening.isTrigger = true;
    }

    // An opening must be active, usable in the gameplay plane, and attached to a valid camera when following it.
    private bool IsOpen => isActiveAndEnabled && Opening.isActiveAndEnabled && Opening.isTrigger
        && Height > 0.001f && Vector3.Dot(transform.forward, Vector3.forward) > 0.9999f
        && (!followCamera || (attachedCamera != null && attachedCamera.isActiveAndEnabled && attachedCamera.orthographic));

    // Refresh both openings and ensure this reciprocal connection belongs to the requested room pair.
    public bool IsValidFor(EnemyDungeonSide room)
    {
        FitToCamera();
        if (linkedPortal != null)
            linkedPortal.FitToCamera();

        return RoomPairValid(room) && IsOpen && linkedPortal != null && linkedPortal != this
            && linkedPortal.linkedPortal == this && linkedPortal.IsOpen
            && gameObject.scene == room.gameObject.scene && linkedPortal.gameObject.scene == gameObject.scene
            && room.Contains(attachedCamera != null
                ? (Vector2)attachedCamera.transform.position
                : Center - Normal * 0.01f)
            && room.OppositeSide.Contains(linkedPortal.attachedCamera != null
                ? (Vector2)linkedPortal.attachedCamera.transform.position
                : linkedPortal.Center - linkedPortal.Normal * 0.01f);
    }

    // Require two active room bounds with usable width and height.
    public static bool RoomPairValid(EnemyDungeonSide room) => room != null && room.OppositeSide != null
        && room.LevelBounds != null && room.OppositeSide.LevelBounds != null
        && room.LevelBounds.isActiveAndEnabled && room.OppositeSide.LevelBounds.isActiveAndEnabled
        && room.LevelBounds.bounds.size.x > 0.001f && room.LevelBounds.bounds.size.y > 0.001f
        && room.OppositeSide.LevelBounds.bounds.size.x > 0.001f && room.OppositeSide.LevelBounds.bounds.size.y > 0.001f;

    // Find an enabled portal in this scene that leads from the requested room to its opposite side.
    public static bool TryFind(Scene scene, EnemyDungeonSide room, out Portal portal)
    {
        foreach (Portal candidate in portals)
            if (candidate != null && candidate.gameObject.scene == scene && candidate.IsValidFor(room))
            {
                portal = candidate;
                return true;
            }

        portal = null;
        return false;
    }

    #endregion

    #region Coordinate mapping and crossing checks

    // Measure distance from the opening; positive values are on the room-facing side.
    public float SignedDistance(Vector2 point) => -Vector2.Dot(point - Center, Normal);

    // Return a position along the opening: 0 is the bottom, 0.5 the center, and 1 the top.
    public Vector2 PointAtHeight(float fraction) => Center + Up * ((fraction - 0.5f) * Height);

    // Convert a world position to the matching position beside the linked portal.
    public Vector2 MapPoint(Vector2 point) => linkedPortal.Center + MapVector(point - Center);

    // Convert a direction or offset so entering this opening becomes emerging from the linked opening.
    public Vector2 MapVector(Vector2 vector) => (-linkedPortal.Normal * Vector2.Dot(vector, Normal)
        + linkedPortal.Up * Vector2.Dot(vector, Up)) * ExitScale;

    // Check whether an object's radius reaches the opening's thickness and height.
    public bool Overlaps(Vector2 point, float radius) => Mathf.Abs(SignedDistance(point)) <= radius + Thickness * 0.5f
        && Mathf.Abs(Vector2.Dot(point - Center, Up)) <= Height * 0.5f + radius;

    // Keep contacts on the room-facing side, optionally limiting them to the opening's height.
    public bool ContainsVisible(Vector2 point, bool clipHeight = true) => SignedDistance(point) >= -0.0001f
        && (!clipHeight || Mathf.Abs(Vector2.Dot(point - Center, Up)) <= Height * 0.5f + 0.0001f);

    // Locate a movement crossing, including when a moving portal has already passed over the object.
    public bool TryCrossing(Vector2 from, Vector2 to, out float fraction, out Vector2 crossing)
    {
        float before = SignedDistance(from), after = SignedDistance(to);
        fraction = 0f;
        crossing = from;

        if (!IsOpen || after > 0f)
            return false;

        if (before > 0f)
        {
            fraction = Mathf.Clamp01(before / (before - after));
            crossing = Vector2.Lerp(from, to, fraction);
        }

        return Mathf.Abs(Vector2.Dot(crossing - Center, Up)) <= Height * 0.5f;
    }

    // Find where a line directed into the portal meets the opening, and how far along it that point lies.
    private bool IntersectLine(Vector2 anchor, Vector2 direction, out Vector2 point, out float distance)
    {
        point = anchor;
        distance = 0f;

        float into = Vector2.Dot(direction, Normal);
        if (into <= 0.00001f)
            return false;

        distance = SignedDistance(anchor) / into;
        point += direction * distance;

        return Mathf.Abs(Vector2.Dot(point - Center, Up)) <= Height * 0.5f;
    }

    #endregion

    #region Laser aiming and beam paths

    // Aim at a local player directly, or map an opposite-room player back through the linked opening.
    public Vector2 AimDirection(Vector2 origin, Vector2 target, bool targetInOtherRoom) =>
        ((targetInOtherRoom ? linkedPortal.MapPoint(target) : target) - origin).normalized;

    // Limit the shot angle to the opening area that fits inside both rooms.
    public Vector2 ClampAim(Vector2 origin, Vector2 desired, EnemyDungeonSide room)
    {
        float low = -Height * 0.5f + 0.02f, high = Height * 0.5f - 0.02f;
        ProjectBounds(room.LevelBounds.bounds, Up, out float roomLow, out float roomHigh);
        low = Mathf.Max(low, roomLow - Vector2.Dot(Center, Up));
        high = Mathf.Min(high, roomHigh - Vector2.Dot(Center, Up));

        linkedPortal.ProjectOpeningRoom(room.OppositeSide.LevelBounds.bounds, out float otherLow, out float otherHigh);
        low = Mathf.Max(low, otherLow / ExitScale);
        high = Mathf.Min(high, otherHigh / ExitScale);

        if (high <= low)
            return Normal;

        float distance = Mathf.Max(0.05f, SignedDistance(origin));
        float baseHeight = Vector2.Dot(origin - Center, Up);
        float into = Vector2.Dot(desired, Normal);
        float slope = into > 0.001f ? Vector2.Dot(desired, Up) / into : 0f;
        slope = Mathf.Clamp(
            slope,
            Mathf.Max(-2.75f, (low - baseHeight) / distance),
            Mathf.Min(2.75f, (high - baseHeight) / distance));

        return (Normal + Up * slope).normalized;
    }

    // Find the usable vertical interval where this opening overlaps a room, leaving a small edge margin.
    private void ProjectOpeningRoom(Bounds bounds, out float low, out float high)
    {
        ProjectBounds(bounds, Up, out low, out high);
        float center = Vector2.Dot(Center, Up);
        low = Mathf.Max(low - center, -Height * 0.5f) + 0.02f;
        high = Mathf.Min(high - center, Height * 0.5f) - 0.02f;
    }

    // Measure a room's minimum and maximum extent along a supplied axis.
    private static void ProjectBounds(Bounds bounds, Vector2 axis, out float low, out float high)
    {
        float center = Vector2.Dot(bounds.center, axis);
        float extent = Mathf.Abs(axis.x) * bounds.extents.x + Mathf.Abs(axis.y) * bounds.extents.y;
        low = center - extent;
        high = center + extent;
    }

    // Trace a straight local beam and, when it reaches the opening, its mapped continuation in the other room.
    public bool BuildBeam(EnemyDungeonSide room, Vector3 origin, Vector2 aim, float width, out BeamPath path)
    {
        path = default;
        if (!IsValidFor(room) || !room.Contains(origin) || aim.sqrMagnitude < 0.0001f)
            return false;

        path.Z = origin.z;
        path.Source = new Segment
        {
            Start = origin,
            End = RoomExit(room.LevelBounds.bounds, origin, aim),
            Width = width,
            Room = room
        };

        if (IntersectLine(origin, aim, out Vector2 hit, out float distance) && distance >= 0f
            && distance <= Vector2.Distance(path.Source.Start, path.Source.End) + 0.001f)
        {
            path.Source.End = hit;
            path.Source.Gate = this;

            Vector2 entry = MapPoint(hit), direction = MapVector(aim).normalized;
            Vector2 unclipped = entry;
            if (ClipEntry(room.OppositeSide.LevelBounds.bounds, ref entry, direction))
            {
                path.HasDestination = true;
                path.Destination = new Segment
                {
                    Start = entry,
                    End = RoomExit(room.OppositeSide.LevelBounds.bounds, entry, direction),
                    Width = width * ExitScale,
                    Room = room.OppositeSide,
                    Gate = Vector2.Distance(entry, unclipped) < 0.001f ? linkedPortal : null
                };
            }
        }

        return true;
    }

    #endregion

    #region Beam rendering and room clipping

    // Update the existing beam renderers, hiding the opposite-room section when the path does not reach it.
    public static void DrawBeam(BeamPath path, LineRenderer source, LineRenderer destination, Color color, float widthFactor)
    {
        DrawSegment(source, path.Source, path.Z, color, widthFactor, true);

        if (path.HasDestination)
            DrawSegment(destination, path.Destination, path.Z, color, widthFactor, false);
        else if (destination != null)
            destination.enabled = false;
    }

    // Draw a two-point segment and extend its portal end just beyond the view to hide small edge gaps.
    private static void DrawSegment(LineRenderer renderer, Segment segment, float z, Color color, float widthFactor, bool source)
    {
        if (renderer == null)
            return;

        float width = segment.Width * widthFactor;
        Vector2 a = segment.Start, b = segment.End;

        if (segment.Gate != null)
        {
            Vector2 along = (source ? b - a : a - b).normalized;
            Portal gate = segment.Gate;
            float padding = 0.5f * width * Mathf.Abs(Vector2.Dot(along, gate.Up))
                / Mathf.Max(0.0001f, Mathf.Abs(Vector2.Dot(along, gate.Normal))) + gate.PixelSize;

            if (source)
                b += along * padding;
            else
                a += along * padding;
        }

        renderer.enabled = true;
        renderer.useWorldSpace = true;
        renderer.positionCount = 2;
        renderer.numCornerVertices = 0;
        renderer.startWidth = renderer.endWidth = width;
        renderer.startColor = renderer.endColor = color;
        renderer.SetPosition(0, new Vector3(a.x, a.y, z));
        renderer.SetPosition(1, new Vector3(b.x, b.y, z));
    }

    // Find where a ray starting inside a room reaches its first horizontal or vertical boundary.
    private static Vector2 RoomExit(Bounds bounds, Vector2 point, Vector2 direction)
    {
        float x = Mathf.Abs(direction.x) > 0.00001f
            ? ((direction.x > 0f ? bounds.max.x : bounds.min.x) - point.x) / direction.x
            : float.PositiveInfinity;
        float y = Mathf.Abs(direction.y) > 0.00001f
            ? ((direction.y > 0f ? bounds.max.y : bounds.min.y) - point.y) / direction.y
            : float.PositiveInfinity;

        return point + direction * Mathf.Max(0f, Mathf.Min(x, y));
    }

    // Advance a mapped beam entry to the room boundary, rejecting rays that never enter that room.
    private static bool ClipEntry(Bounds bounds, ref Vector2 point, Vector2 direction)
    {
        float near = 0f, far = float.PositiveInfinity;
        for (int axis = 0; axis < 2; axis++)
        {
            if (Mathf.Abs(direction[axis]) < 0.00001f)
            {
                if (point[axis] < bounds.min[axis] || point[axis] > bounds.max[axis])
                    return false;

                continue;
            }

            float a = (bounds.min[axis] - point[axis]) / direction[axis], b = (bounds.max[axis] - point[axis]) / direction[axis];
            near = Mathf.Max(near, Mathf.Min(a, b));
            far = Mathf.Min(far, Mathf.Max(a, b));
        }

        if (near > far || far < 0f)
            return false;

        point += direction * near;
        return true;
    }

    #endregion

    #region Bat registration, transfer, and damage paths

    // Track a live Bat so portals can display its crossing copy.
    public static void RegisterBat(BatController bat)
    {
        bats.Add(bat);
    }

    // Remove the Bat's shared tracking and every portal-owned copy when it stops participating.
    public static void UnregisterBat(BatController bat)
    {
        bats.Remove(bat);
        lastExit.Remove(bat);

        foreach (Portal portal in portals)
            if (portal != null)
                portal.RemoveCopy(bat);
    }

    // Transfer a swooping Bat through a valid opening, then let its controller build the next room's pass.
    public bool TryTransfer(BatController bat, Vector2 crossing, Vector2 direction, out Transit transit)
    {
        transit = default;
        if (bat == null || !bat.IsSwooping || !IsValidFor(bat.CurrentSide))
            return false;

        ReleaseExit(bat, bat.Position);
        if (lastExit.TryGetValue(bat, out Portal blocked) && blocked == this && Overlaps(bat.Position, bat.PortalRadius))
            return false;

        if (SignedDistance(crossing) > 0.001f || Mathf.Abs(Vector2.Dot(crossing - Center, Up)) > Height * 0.5f)
            return false;

        Vector2 destination = MapPoint(crossing);
        Bounds bounds = bat.CurrentSide.OppositeSide.LevelBounds.bounds;
        if (destination.x < bounds.min.x - 0.001f || destination.x > bounds.max.x + 0.001f
            || destination.y < bounds.min.y - 0.001f || destination.y > bounds.max.y + 0.001f)
            return false;

        transit = new Transit
        {
            Exit = linkedPortal,
            Position = destination,
            Direction = MapVector(direction).normalized,
            Scale = ExitScale
        };

        lastExit[bat] = linkedPortal;
        bat.GetComponent<Rigidbody2D>().position = destination;
        bat.transform.position = new Vector3(destination.x, destination.y, bat.transform.position.z);
        bat.transform.localScale *= transit.Scale;
        bat.ReceivePortalTransfer(transit);
        return true;
    }

    // Allow a later return transfer once the Bat has cleared its previous exit, or that exit closes.
    private static void ReleaseExit(BatController bat, Vector2 position)
    {
        if (lastExit.TryGetValue(bat, out Portal exit) && (exit == null || !exit.IsOpen || !exit.Overlaps(position, bat.PortalRadius)))
            lastExit.Remove(bat);
    }

    // Describe Bat movement on both sides of an overlapping opening for the controller's contact-damage checks.
    public bool TravelSegments(BatController bat, Vector2 from, Vector2 to, out Segment source, out Segment other)
    {
        EnemyDungeonSide room = bat.CurrentSide;
        float radius = bat.PortalRadius;
        ReleaseExit(bat, from);

        source = new Segment
        {
            Start = from,
            End = to,
            Width = radius * 2f,
            Room = room,
            Gate = this,
            ClipHeight = true
        };
        other = default;

        if (!IsValidFor(room) || (!Overlaps(from, radius) && !Overlaps(to, radius) && !TryCrossing(from, to, out _, out _)))
            return false;

        other = new Segment
        {
            Start = MapPoint(from),
            End = MapPoint(to),
            Width = radius * 2f * ExitScale,
            Room = room.OppositeSide,
            Gate = linkedPortal,
            ClipHeight = true
        };
        return true;
    }

    #endregion

    #region Bat visual copies and cleanup

    // Refresh the opening and show mapped Bat copies only while their source overlaps this portal.
    private void LateUpdate()
    {
        FitToCamera();

        // Hide last frame's copies before deciding which Bats still overlap the opening.
        foreach (VisualCopy copy in copies.Values)
            if (copy.Renderer != null)
                copy.Renderer.enabled = false;

        foreach (BatController bat in bats)
        {
            if (bat == null || !bat.IsSwooping || !IsValidFor(bat.CurrentSide))
                continue;

            MeshRenderer source = copies.TryGetValue(bat, out VisualCopy copy)
                ? copy.Source
                : bat.GetComponentInChildren<MeshRenderer>(true);
            if (source == null || !source.enabled
                || !Overlaps(source.transform.position, Mathf.Max(source.bounds.extents.x, source.bounds.extents.y)))
                continue;

            // Create a render-only copy once, sharing the Bat's existing mesh and material.
            if (copy == null)
            {
                var obj = new GameObject(bat.name + " portal copy");
                SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
                obj.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;

                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = source.sharedMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                copy = new VisualCopy { Source = source, Renderer = renderer };
                copies.Add(bat, copy);
            }

            // Match the Bat's mapped pose, size, draw order, and state color at the exit.
            Vector2 mapped = MapPoint(source.transform.position);
            copy.Renderer.transform.SetPositionAndRotation(
                new Vector3(mapped.x, mapped.y, source.transform.position.z),
                linkedPortal.transform.rotation * Quaternion.Inverse(transform.rotation) * source.transform.rotation);
            copy.Renderer.transform.localScale = source.transform.lossyScale * ExitScale;
            copy.Renderer.sortingLayerID = source.sortingLayerID;
            copy.Renderer.sortingOrder = source.sortingOrder;
            source.GetPropertyBlock(copy.Properties);
            copy.Renderer.SetPropertyBlock(copy.Properties);
            copy.Renderer.enabled = true;
        }
    }

    // Destroy this portal's copy of one Bat and remove its cached reference.
    private void RemoveCopy(BatController bat)
    {
        if (!copies.TryGetValue(bat, out VisualCopy copy))
            return;

        DestroyCopy(copy);
        copies.Remove(bat);
    }

    // Destroy all visual copies owned by this portal.
    private void ClearCopies()
    {
        foreach (VisualCopy copy in copies.Values)
            DestroyCopy(copy);

        copies.Clear();
    }

    // Hide a copy immediately, then destroy its object using the appropriate play-mode or editor operation.
    private static void DestroyCopy(VisualCopy copy)
    {
        if (copy.Renderer == null)
            return;

        copy.Renderer.enabled = false;
        if (Application.isPlaying)
            Destroy(copy.Renderer.gameObject);
        else
            DestroyImmediate(copy.Renderer.gameObject);
    }

    #endregion
}

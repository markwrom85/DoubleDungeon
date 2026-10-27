using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public class LazerEyeballLaser : MonoBehaviour
{
    [SerializeField] private LineRenderer roomBeam, oppositeBeam;
    [SerializeField, Range(0.05f, 1f)] private float widthRelativeToBody = 0.6666667f;
    [SerializeField, Range(0.01f, 1f)] private float warningWidth = 0.15f;
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.1f, 0.7f);
    [SerializeField] private Color firingColor = Color.red;
    private EnemyDungeonSide side;
    private Portal portal;
    private CircleCollider2D bodyCollider;
    private SpriteRenderer bodySprite;
    private bool onLeft, showing, firing;
    private Vector2 direction;
    private float damage;
    private BeamPath path;
    private PortalTraveller traveller;
    private struct Segment
    {
        public Vector2 Start, End;
        public float Width;
        public Portal Gate;
    }
    private struct BeamPath
    {
        public Segment Source, Destination;
        public bool HasDestination;
        public float Z;
    }
    private readonly HashSet<PlayerInput> hitPlayers = new HashSet<PlayerInput>();
    private readonly List<Collider2D> colliders = new List<Collider2D>(8);

    public bool IsFiring => firing;
    public Vector2 Start => path.Source.Start;
    public Vector2 End => path.Source.End;
    public Vector2 OppositeStart => path.Destination.Start;
    public Vector2 OppositeEnd => path.Destination.End;
    public bool HasOppositeSegment => showing && path.HasDestination;

    private void Awake()
    {
        bodyCollider = GetComponent<CircleCollider2D>(); bodySprite = GetComponent<SpriteRenderer>();
        traveller = GetComponent<PortalTraveller>();
        Hide();
    }

    public void Configure(EnemyDungeonSide room, bool isLeft)
    { Hide(); side = room; onLeft = isLeft; portal = null; }

    private bool FindPortal() => side != null && side.OppositeSide != null
        && ((portal != null && portal.IsValidFor(side.LevelBounds, side.OppositeSide.LevelBounds))
            || Portal.TryFind(gameObject.scene, side.LevelBounds, side.OppositeSide.LevelBounds, out portal));

    public Vector2 GetAimDirection(PlayerInput player)
    {
        if (player == null || !FindPortal()) return onLeft ? Vector2.right : Vector2.left;
        Vector2 target = side.IsValidTarget(player) ? (Vector2)player.transform.position
            : portal.LinkedPortal.MapPoint(player.transform.position);
        return (target - (Vector2)transform.position).normalized;
    }

    public Vector2 ClampDirection(Vector2 desired) => FindPortal()
        ? ClampAim(desired) : onLeft ? Vector2.right : Vector2.left;

    public void ShowWarning(Vector2 aim)
    {
        showing = true; firing = false; direction = aim.normalized;
        Rebuild();
    }

    public void BeginFire(Vector2 aim, float shotDamage)
    {
        hitPlayers.Clear(); direction = aim.normalized; damage = Mathf.Max(0f, shotDamage);
        showing = firing = true;
    }

    private void LateUpdate()
    { if (showing && Rebuild() && firing) DamagePlayers(); }

    private bool Rebuild()
    {
        float diameter = bodySprite != null && bodySprite.sprite != null ? bodySprite.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x)
            : bodyCollider != null ? bodyCollider.radius * 2f * Mathf.Abs(transform.lossyScale.x) : 0.5f;
        if (!FindPortal() || !BuildPath(diameter * widthRelativeToBody))
        { Hide(); return false; }
        Color color = firing ? firingColor : warningColor;
        float widthFactor = firing ? 1f : warningWidth;
        DrawSegment(roomBeam, path.Source, color, widthFactor, true);
        if (path.HasDestination) DrawSegment(oppositeBeam, path.Destination, color, widthFactor, false);
        else if (oppositeBeam != null) oppositeBeam.enabled = false;
        return true;
    }

    private void DamagePlayers()
    {
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (hitPlayers.Contains(player)) continue;
            bool local = side.IsValidTarget(player);
            if (!local && (!path.HasDestination || !side.OppositeSide.IsValidTarget(player))) continue;
            if (!Touches(player, local ? path.Source : path.Destination)) continue;
            PlayerInfo info = player.GetComponentInChildren<PlayerInfo>(true);
            if (info == null) continue;
            hitPlayers.Add(player); info.TakeDamage(damage);
        }
    }

    private bool Touches(PlayerInput player, Segment segment)
    {
        Vector2 delta = segment.End - segment.Start;
        player.GetComponentsInChildren(false, colliders);
        foreach (Collider2D collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger) continue;
            float along = delta.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - segment.Start, delta) / delta.sqrMagnitude) : 0f;
            Vector2 point = segment.Start + delta * along, contact = collider.ClosestPoint(point);
            if ((segment.Gate == null || segment.Gate.ContainsVisible(contact)) && (contact - point).sqrMagnitude <= segment.Width * segment.Width * 0.25f) return true;
        }
        return false;
    }

    public void Hide()
    {
        showing = firing = false; path = default; hitPlayers.Clear();
        if (roomBeam != null) roomBeam.enabled = false;
        if (oppositeBeam != null) oppositeBeam.enabled = false;
    }
    // Keep this enemy's aim inside the usable part of both rooms.
    private Vector2 ClampAim(Vector2 desired)
    {
        Vector2 origin = transform.position;
        float low = -portal.Height * 0.5f + 0.02f, high = portal.Height * 0.5f - 0.02f;
        ProjectBounds(side.LevelBounds.bounds, portal.Up, out float roomLow, out float roomHigh);
        low = Mathf.Max(low, roomLow - Vector2.Dot(portal.Center, portal.Up));
        high = Mathf.Min(high, roomHigh - Vector2.Dot(portal.Center, portal.Up));

        ProjectOpeningRoom(portal.LinkedPortal, side.OppositeSide.LevelBounds.bounds, out float otherLow, out float otherHigh);
        low = Mathf.Max(low, otherLow / portal.ExitScale);
        high = Mathf.Min(high, otherHigh / portal.ExitScale);

        if (high <= low)
            return portal.Normal;

        float distance = Mathf.Max(0.05f, portal.SignedDistance(origin));
        float baseHeight = Vector2.Dot(origin - portal.Center, portal.Up);
        float into = Vector2.Dot(desired, portal.Normal);
        float slope = into > 0.001f ? Vector2.Dot(desired, portal.Up) / into : 0f;
        slope = Mathf.Clamp(
            slope,
            Mathf.Max(-2.75f, (low - baseHeight) / distance),
            Mathf.Min(2.75f, (high - baseHeight) / distance));

        return (portal.Normal + portal.Up * slope).normalized;
    }

    // Calculate the opening interval available to this attack.
    private void ProjectOpeningRoom(Portal gate, Bounds bounds, out float low, out float high)
    {
        ProjectBounds(bounds, gate.Up, out low, out high);
        float center = Vector2.Dot(gate.Center, gate.Up);
        low = Mathf.Max(low - center, -gate.Height * 0.5f) + 0.02f;
        high = Mathf.Min(high - center, gate.Height * 0.5f) - 0.02f;
    }

    // Project room extents onto the opening's vertical axis.
    private static void ProjectBounds(Bounds bounds, Vector2 axis, out float low, out float high)
    {
        float center = Vector2.Dot(bounds.center, axis);
        float extent = Mathf.Abs(axis.x) * bounds.extents.x + Mathf.Abs(axis.y) * bounds.extents.y;
        low = center - extent;
        high = center + extent;
    }

    // Limit this attack to its room bounds and ask the portal to route the ray when permitted.
    private bool BuildPath(float width)
    {
        path = default;
        Vector2 origin = transform.position;
        if (!side.Contains(origin) || direction.sqrMagnitude < 0.0001f) return false;
        path.Z = transform.position.z;
        path.Source = new Segment { Start = origin, End = RoomExit(side.LevelBounds.bounds, origin, direction), Width = width };
        float range = Vector2.Distance(path.Source.Start, path.Source.End);
        if (portal.TryMapRay(traveller, origin, direction, range, out Vector2 hit, out Vector2 entry, out Vector2 exitDirection))
        {
            path.Source.End = hit;
            path.Source.Gate = portal;
            Vector2 unclipped = entry;
            if (ClipEntry(side.OppositeSide.LevelBounds.bounds, ref entry, exitDirection))
            {
                path.HasDestination = true;
                path.Destination = new Segment { Start = entry,
                    End = RoomExit(side.OppositeSide.LevelBounds.bounds, entry, exitDirection),
                    Width = width * portal.ExitScale,
                    Gate = Vector2.Distance(entry, unclipped) < 0.001f ? portal.LinkedPortal : null };
            }
        }
        return true;
    }

    // Draw the attack's straight segment with a small extension to cover the edge.
    private void DrawSegment(LineRenderer renderer, Segment segment, Color color, float widthFactor, bool source)
    {
        if (renderer == null)
            return;

        float z = path.Z;
        float width = segment.Width * widthFactor;
        Vector2 a = segment.Start, b = segment.End;

        if (segment.Gate != null)
        {
            Vector2 along = (source ? b - a : a - b).normalized;
            Portal gate = segment.Gate;
            float padding = 0.5f * width * Mathf.Abs(Vector2.Dot(along, gate.Up))
                / Mathf.Max(0.0001f, Mathf.Abs(Vector2.Dot(along, gate.Normal))) + (gate.AttachedCamera != null && gate.AttachedCamera.pixelHeight > 0 ? gate.Height / gate.AttachedCamera.pixelHeight : 0.002f);

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

    // Stop this attack at the room boundary.
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

    // Trim a mapped ray to the destination room.
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

    private void OnDisable() { Hide(); }
}

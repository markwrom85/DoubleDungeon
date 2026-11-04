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
    private PortalTraveller traveller;
    private PortalBeam Path => traveller.Beam;
    private readonly HashSet<PlayerInput> hitPlayers = new HashSet<PlayerInput>();
    private readonly List<Collider2D> colliders = new List<Collider2D>(8);

    public bool IsFiring => firing;
    public Vector2 Start => Path.Segments.Count > 0 ? Path.Segments[0].Start : Vector2.zero;
    public Vector2 End => Path.Segments.Count > 0 ? Path.Segments[0].End : Vector2.zero;
    public Vector2 OppositeStart => Path.Segments.Count > 1 ? Path.Segments[1].Start : Vector2.zero;
    public Vector2 OppositeEnd => Path.Segments.Count > 1 ? Path.Segments[1].End : Vector2.zero;
    public bool HasOppositeSegment => showing && Path.Segments.Count > 1;

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
        if (!FindPortal() || !side.Contains(transform.position) || traveller == null
            || !traveller.TraceBeam(transform.position, direction, float.PositiveInfinity,
                diameter * widthRelativeToBody, side.LevelBounds, side.OppositeSide.LevelBounds))
        { Hide(); return false; }
        Color color = firing ? firingColor : warningColor;
        float widthFactor = firing ? 1f : warningWidth;
        Path.DrawLine(roomBeam, 0, transform.position.z, color, widthFactor);
        Path.DrawLine(oppositeBeam, 1, transform.position.z, color, widthFactor);
        return true;
    }

    private void DamagePlayers()
    {
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (hitPlayers.Contains(player)) continue;
            bool local = side.IsValidTarget(player);
            if (!local && (!HasOppositeSegment || !side.OppositeSide.IsValidTarget(player))) continue;
            if (!Touches(player, Path.Segments[local ? 0 : 1])) continue;
            PlayerInfo info = player.GetComponentInChildren<PlayerInfo>(true);
            if (info == null) continue;
            hitPlayers.Add(player); info.TakeDamage(damage);
        }
    }

    // Player selection and the per-burst hit limit belong to this attack; geometry belongs to the traveller.
    private bool Touches(PlayerInput player, PortalBeam.Segment segment)
    {
        player.GetComponentsInChildren(false, colliders);
        foreach (Collider2D collider in colliders)
            if (segment.Touches(collider)) return true;
        return false;
    }

    public void Hide()
    {
        showing = firing = false; hitPlayers.Clear();
        if (traveller != null) Path.Clear();
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

    private void OnDisable() { Hide(); }
}

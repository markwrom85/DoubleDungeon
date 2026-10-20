using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// I map my laser between paired rooms and damage each player once per shot.
public class LazerEyeballLaser : MonoBehaviour
{
    [SerializeField] private LineRenderer roomBeam, oppositeBeam;
    [SerializeField] private Shader roomBeamShader;
    [SerializeField, Range(0.05f, 1f)] private float widthRelativeToBody = 0.6666667f;
    [SerializeField, Range(0.01f, 1f)] private float warningWidth = 0.15f;
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.1f, 0.7f);
    [SerializeField] private Color firingColor = Color.red;

    private EnemyDungeonSide side;
    private CircleCollider2D bodyCollider;
    private SpriteRenderer bodySprite;
    private bool onLeft, firing, hasOppositeSegment;
    private Vector2 direction, start, end, oppositeStart, oppositeEnd;
    private float damage, width, oppositeWidth;
    private readonly HashSet<PlayerInput> hitPlayers = new HashSet<PlayerInput>();
    private Material beamMaterial;
    private MaterialPropertyBlock beamProperties;
    private static readonly int RoomBoundsId = Shader.PropertyToID("_RoomBounds");

    public bool IsFiring => firing;
    public Vector2 Start => start;
    public Vector2 End => end;
    public Vector2 OppositeStart => oppositeStart;
    public Vector2 OppositeEnd => oppositeEnd;
    public bool HasOppositeSegment => hasOppositeSegment;

    private void Awake()
    {
        bodyCollider = GetComponent<CircleCollider2D>();
        bodySprite = GetComponent<SpriteRenderer>();
        beamProperties = new MaterialPropertyBlock();
        if (roomBeamShader != null) beamMaterial = new Material(roomBeamShader);
        PrepareRenderer(roomBeam);
        PrepareRenderer(oppositeBeam);
        Hide();
    }

    private void PrepareRenderer(LineRenderer beam)
    {
        if (beam == null) return;
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.enabled = false;
        if (beamMaterial != null) beam.sharedMaterial = beamMaterial;
    }

    public void Configure(EnemyDungeonSide room, bool isLeft)
    {
        Hide();
        side = room;
        onLeft = isLeft;
    }

    private bool HasRoomPair => side != null && side.OppositeSide != null
        && side.LevelBounds != null && side.OppositeSide.LevelBounds != null
        && side.LevelBounds.isActiveAndEnabled && side.OppositeSide.LevelBounds.isActiveAndEnabled
        && side.LevelBounds.bounds.size.x > 0.001f && side.LevelBounds.bounds.size.y > 0.001f
        && side.OppositeSide.LevelBounds.bounds.size.x > 0.001f
        && side.OppositeSide.LevelBounds.bounds.size.y > 0.001f;

    public Vector2 GetAimDirection(PlayerInput player)
    {
        if (!HasRoomPair || player == null) return onLeft ? Vector2.right : Vector2.left;
        Vector2 aim = player.transform.position;
        if (!side.IsValidTarget(player))
        {
            // Unfold the destination through the room border, using the same uniform scale as the views.
            Bounds from = side.LevelBounds.bounds, to = side.OppositeSide.LevelBounds.bounds;
            float scale = from.size.y / to.size.y;
            float fromEdge = onLeft ? from.max.x : from.min.x;
            float toEdge = onLeft ? to.min.x : to.max.x;
            aim = new Vector2(fromEdge + (aim.x - toEdge) * scale,
                from.min.y + (aim.y - to.min.y) * scale);
        }
        return (aim - (Vector2)transform.position).normalized;
    }

    public Vector2 ClampDirection(Vector2 desired)
    {
        float inward = onLeft ? 1f : -1f;
        if (side == null || side.LevelBounds == null) return new Vector2(inward, 0f);
        Bounds bounds = side.LevelBounds.bounds;
        float seam = onLeft ? bounds.max.x : bounds.min.x;
        float low = bounds.min.y + 0.02f, high = bounds.max.y - 0.02f;
        float distance = Mathf.Max(0.05f, (seam - transform.position.x) * inward);
        float slope = desired.x * inward > 0.001f ? desired.y / Mathf.Abs(desired.x) : 0f;
        float minSlope = (low - transform.position.y) / distance;
        float maxSlope = (high - transform.position.y) / distance;
        slope = Mathf.Clamp(slope, Mathf.Max(-2.75f, minSlope), Mathf.Min(2.75f, maxSlope));
        return new Vector2(inward, slope).normalized;
    }

    public void ShowWarning(Vector2 aim)
    {
        firing = false;
        direction = aim;
        Rebuild();
    }

    public void BeginFire(Vector2 lockedAim, float shotDamage)
    {
        // Capture both world-space segments once. Camera movement cannot redirect a live shot.
        hitPlayers.Clear();
        direction = lockedAim;
        damage = Mathf.Max(0f, shotDamage);
        firing = true;
        Rebuild();
    }

    private void FixedUpdate()
    {
        if (!firing) return;
        if (!HasRoomPair) { Hide(); return; }
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (hitPlayers.Contains(player)) continue;
            bool local = side != null && side.IsValidTarget(player);
            bool opposite = hasOppositeSegment && side.OppositeSide.IsValidTarget(player);
            if (!local && !opposite) continue;
            if (!TouchesPlayer(player, local ? start : oppositeStart, local ? end : oppositeEnd,
                local ? width : oppositeWidth)) continue;
            PlayerInfo info = player.GetComponentInChildren<PlayerInfo>(true);
            if (info == null) continue;
            hitPlayers.Add(player);
            info.TakeDamage(damage);
        }
    }

    private bool TouchesPlayer(PlayerInput player, Vector2 a, Vector2 b, float beamWidth)
    {
        // I test this player's colliders directly so walls and other enemies cannot consume the beam's hit buffer.
        Vector2 segment = b - a;
        player.GetComponentsInChildren(false, colliderList);
        for (int index = 0; index < colliderList.Count; index++)
        {
            Collider2D collider = colliderList[index];
            if (!collider.enabled || collider.isTrigger) continue;
            Vector2 center = collider.bounds.center;
            float along = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(center - a, segment) / segment.sqrMagnitude) : 0f;
            Vector2 point = a + segment * along;
            if ((collider.ClosestPoint(point) - point).sqrMagnitude <= beamWidth * beamWidth * 0.25f) return true;
        }
        return false;
    }
    private readonly List<Collider2D> colliderList = new List<Collider2D>(8);

    private void Rebuild()
    {
        hasOppositeSegment = false;
        if (!HasRoomPair || !side.Contains(transform.position))
        { Hide(); return; }
        start = transform.position;
        Bounds bounds = side.LevelBounds.bounds;
        end = GetExit(bounds, start, direction);
        float diameter = bodySprite != null && bodySprite.sprite != null ? bodySprite.sprite.bounds.size.x
            * Mathf.Abs(transform.lossyScale.x) : bodyCollider != null ? bodyCollider.radius * 2f
            * Mathf.Abs(transform.lossyScale.x) : 0.5f;
        width = diameter * widthRelativeToBody;
        oppositeWidth = width;
        float inward = onLeft ? 1f : -1f;
        float seam = onLeft ? bounds.max.x : bounds.min.x;
        if (direction.x * inward > 0.001f)
        {
            float distance = (seam - start.x) / direction.x;
            Vector2 edge = start + direction * distance;
            if (distance >= 0f && edge.y >= bounds.min.y && edge.y <= bounds.max.y)
            {
                end = edge;
                Bounds other = side.OppositeSide.LevelBounds.bounds;
                float y = (edge.y - bounds.min.y) / bounds.size.y;
                oppositeStart = new Vector2(onLeft ? other.min.x : other.max.x, other.min.y + y * other.size.y);
                oppositeWidth *= other.size.y / bounds.size.y;
                hasOppositeSegment = ClipEntry(other, ref oppositeStart, direction);
                if (hasOppositeSegment) oppositeEnd = GetExit(other, oppositeStart, direction);
            }
        }
        Draw(roomBeam, start, end, width, bounds, false, hasOppositeSegment);
        if (hasOppositeSegment) Draw(oppositeBeam, oppositeStart, oppositeEnd, oppositeWidth,
            side.OppositeSide.LevelBounds.bounds, true, false);
        else if (oppositeBeam != null) oppositeBeam.enabled = false;
    }

    private static bool ClipEntry(Bounds bounds, ref Vector2 point, Vector2 aim)
    {
        // I clip an offscreen entry to the room itself instead of letting a beam escape its boundaries.
        float near = 0f, far = float.PositiveInfinity;
        for (int axis = 0; axis < 2; axis++)
        {
            if (Mathf.Abs(aim[axis]) < 0.00001f)
            { if (point[axis] < bounds.min[axis] || point[axis] > bounds.max[axis]) return false; continue; }
            float first = (bounds.min[axis] - point[axis]) / aim[axis];
            float second = (bounds.max[axis] - point[axis]) / aim[axis];
            near = Mathf.Max(near, Mathf.Min(first, second));
            far = Mathf.Min(far, Mathf.Max(first, second));
        }
        if (near > far || far < 0f) return false;
        point += aim * near;
        return true;
    }

    private static Vector2 GetExit(Bounds bounds, Vector2 point, Vector2 aim)
    {
        float x = Mathf.Abs(aim.x) > 0.00001f ? ((aim.x > 0f ? bounds.max.x : bounds.min.x) - point.x) / aim.x : float.PositiveInfinity;
        float y = Mathf.Abs(aim.y) > 0.00001f ? ((aim.y > 0f ? bounds.max.y : bounds.min.y) - point.y) / aim.y : float.PositiveInfinity;
        return point + aim * Mathf.Max(0f, Mathf.Min(x, y));
    }

    private void Draw(LineRenderer beam, Vector2 a, Vector2 b, float beamWidth, Bounds bounds,
        bool extendStart, bool extendEnd)
    {
        if (beam == null) return;
        beam.enabled = true;
        float visibleWidth = beamWidth * (firing ? 1f : warningWidth);
        beam.startWidth = beam.endWidth = visibleWidth;
        beam.startColor = beam.endColor = firing ? firingColor : warningColor;
        if (beamMaterial != null)
        {
            beam.GetPropertyBlock(beamProperties);
            beamProperties.SetVector(RoomBoundsId, new Vector4(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y));
            beam.SetPropertyBlock(beamProperties);
            // LineRenderer ends are perpendicular to the ray. Overdraw and clip at the vertical
            // room border so diagonal beams keep their full thickness through the divider.
            Vector2 along = (b - a).normalized;
            float padding = 0.5f * visibleWidth * Mathf.Abs(along.y) / Mathf.Max(0.001f, Mathf.Abs(along.x)) + 0.001f;
            if (extendStart) a -= along * padding;
            if (extendEnd) b += along * padding;
        }
        beam.SetPosition(0, new Vector3(a.x, a.y, transform.position.z));
        beam.SetPosition(1, new Vector3(b.x, b.y, transform.position.z));
    }

    public void Hide()
    {
        firing = hasOppositeSegment = false;
        hitPlayers.Clear();
        if (roomBeam != null) roomBeam.enabled = false;
        if (oppositeBeam != null) oppositeBeam.enabled = false;
    }
    private void OnDisable() { Hide(); }
    private void OnDestroy() { if (beamMaterial != null) Destroy(beamMaterial); }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// I map my laser between paired rooms and damage each player once per shot.
public class LazerEyeballLaser : MonoBehaviour
{
    [SerializeField] private LineRenderer roomBeam, oppositeBeam;
    [SerializeField] private Camera leftView, rightView;
    [SerializeField] private bool alignWithCameras = false;
    [SerializeField, Range(0.05f, 1f)] private float widthRelativeToBody = 0.6666667f;
    [SerializeField, Range(0.01f, 1f)] private float warningWidth = 0.15f;
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.1f, 0.7f);
    [SerializeField] private Color firingColor = Color.red;

    private EnemyDungeonSide side;
    private CircleCollider2D bodyCollider;
    private SpriteRenderer bodySprite;
    private bool onLeft, showing, firing, hasOppositeSegment;
    private Vector2 direction, start, end, oppositeStart, oppositeEnd;
    private float damage, width, oppositeWidth;
    private readonly HashSet<PlayerInput> hitPlayers = new HashSet<PlayerInput>();

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
        // I find the existing split-screen views once and only read their projection during aiming.
        foreach (Camera view in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (view.gameObject.scene != gameObject.scene) continue;
            if (leftView == null && view.name == "LeftCameraBrain") leftView = view;
            if (rightView == null && view.name == "RightCameraBrain") rightView = view;
        }
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
    }

    public void Configure(EnemyDungeonSide room, bool isLeft)
    {
        Hide();
        side = room;
        onLeft = isLeft;
    }

    private bool CanUseViews()
    {
        // I anchor my beam to the rooms unless I explicitly enable camera alignment.
        if (!alignWithCameras || side == null || side.OppositeSide == null || leftView == null || rightView == null) return false;
        Camera from = onLeft ? leftView : rightView;
        Camera to = onLeft ? rightView : leftView;
        return from.isActiveAndEnabled && to.isActiveAndEnabled && from.orthographic && to.orthographic
            && from.targetDisplay == to.targetDisplay && from.pixelRect.width > 0f && to.pixelRect.width > 0f
            && side.Contains(from.transform.position) && side.OppositeSide.Contains(to.transform.position);
    }

    public Vector2 GetAimDirection(PlayerInput player)
    {
        if (side == null || player == null) return onLeft ? Vector2.right : Vector2.left;
        Vector2 aim = player.transform.position;
        if (CanUseViews())
        {
            Camera from = onLeft ? leftView : rightView;
            Camera targetView = side.IsValidTarget(player) ? from : onLeft ? rightView : leftView;
            Vector3 screen = targetView.WorldToScreenPoint(player.transform.position);
            screen.z = Mathf.Abs(transform.position.z - from.transform.position.z);
            aim = from.ScreenToWorldPoint(screen);
        }
        else if (!side.IsValidTarget(player) && side.OppositeSide != null)
        {
            // I place the other room beside mine in a shared aiming plane using their room bounds.
            Bounds from = side.LevelBounds.bounds, to = side.OppositeSide.LevelBounds.bounds;
            float x = (aim.x - to.min.x) / to.size.x;
            float y = (aim.y - to.min.y) / to.size.y;
            aim = new Vector2((onLeft ? from.max.x : from.min.x - from.size.x) + x * from.size.x,
                from.min.y + y * from.size.y);
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
        if (CanUseViews())
        {
            Camera from = onLeft ? leftView : rightView;
            Camera to = onLeft ? rightView : leftView;
            float depth = Mathf.Abs(transform.position.z - from.transform.position.z);
            Vector3 edge = from.ViewportToWorldPoint(new Vector3(onLeft ? 1f : 0f, 0.5f, depth));
            seam = onLeft ? Mathf.Min(seam, edge.x) : Mathf.Max(seam, edge.x);
            // I keep the seam inside both views so my warning and shot continue into the other room.
            Bounds other = side.OppositeSide.LevelBounds.bounds;
            Vector3 bottom = to.WorldToScreenPoint(new Vector3(other.center.x, other.min.y, transform.position.z));
            Vector3 top = to.WorldToScreenPoint(new Vector3(other.center.x, other.max.y, transform.position.z));
            bottom.y = Mathf.Max(bottom.y, Mathf.Max(from.pixelRect.yMin, to.pixelRect.yMin));
            top.y = Mathf.Min(top.y, Mathf.Min(from.pixelRect.yMax, to.pixelRect.yMax));
            bottom.z = top.z = depth;
            low = Mathf.Max(low, from.ScreenToWorldPoint(bottom).y + 0.02f);
            high = Mathf.Min(high, from.ScreenToWorldPoint(top).y - 0.02f);
        }
        float distance = Mathf.Max(0.05f, (seam - transform.position.x) * inward);
        float slope = desired.x * inward > 0.001f ? desired.y / Mathf.Abs(desired.x) : 0f;
        float minSlope = (low - transform.position.y) / distance;
        float maxSlope = (high - transform.position.y) / distance;
        slope = Mathf.Clamp(slope, Mathf.Max(-2.75f, minSlope), Mathf.Min(2.75f, maxSlope));
        return new Vector2(inward, slope).normalized;
    }

    public void ShowWarning(Vector2 aim)
    {
        showing = true;
        firing = false;
        direction = aim;
        Rebuild();
    }

    public void BeginFire(Vector2 lockedAim, float shotDamage)
    {
        // I clear the previous shot's hit list and keep this direction fixed until the shot ends.
        hitPlayers.Clear();
        direction = lockedAim;
        damage = Mathf.Max(0f, shotDamage);
        showing = firing = true;
        Rebuild();
    }

    private void LateUpdate() { if (showing) Rebuild(); }
    private void FixedUpdate()
    {
        if (!firing) return;
        Rebuild();
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
        if (side == null || side.LevelBounds == null || side.OppositeSide == null || !side.Contains(transform.position))
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
        bool views = CanUseViews();
        Camera from = onLeft ? leftView : rightView;
        Camera to = onLeft ? rightView : leftView;
        if (views)
        {
            float depth = Mathf.Abs(transform.position.z - from.transform.position.z);
            float screenEdge = from.ViewportToWorldPoint(new Vector3(onLeft ? 1f : 0f, 0.5f, depth)).x;
            seam = onLeft ? Mathf.Min(seam, screenEdge) : Mathf.Max(seam, screenEdge);
        }
        if (direction.x * inward > 0.001f)
        {
            float distance = (seam - start.x) / direction.x;
            Vector2 edge = start + direction * distance;
            if (distance >= 0f && edge.y >= bounds.min.y && edge.y <= bounds.max.y)
            {
                // I retain the full local hit segment while the camera clips its visible portion at the divider.
                if (!views) end = edge;
                Bounds other = side.OppositeSide.LevelBounds.bounds;
                Vector2 nextDirection;
                if (views)
                {
                    Vector3 screen = from.WorldToScreenPoint(new Vector3(edge.x, edge.y, transform.position.z));
                    screen.x = onLeft ? to.pixelRect.xMin : to.pixelRect.xMax;
                    screen.z = Mathf.Abs(transform.position.z - to.transform.position.z);
                    oppositeStart = to.ScreenToWorldPoint(screen);
                    Vector3 delta = from.WorldToScreenPoint(transform.position + (Vector3)direction)
                        - from.WorldToScreenPoint(transform.position);
                    Vector3 next = screen + new Vector3(delta.x, delta.y, 0f);
                    nextDirection = ((Vector2)to.ScreenToWorldPoint(next) - oppositeStart).normalized;
                    oppositeWidth *= to.orthographicSize / from.orthographicSize;
                }
                else
                {
                    float y = (edge.y - bounds.min.y) / bounds.size.y;
                    oppositeStart = new Vector2(onLeft ? other.min.x : other.max.x, other.min.y + y * other.size.y);
                    nextDirection = new Vector2(direction.x * other.size.x / bounds.size.x,
                        direction.y * other.size.y / bounds.size.y).normalized;
                    oppositeWidth *= other.size.y / bounds.size.y;
                }
                hasOppositeSegment = ClipEntry(other, ref oppositeStart, nextDirection);
                if (hasOppositeSegment) oppositeEnd = GetExit(other, oppositeStart, nextDirection);
            }
        }
        Draw(roomBeam, start, end, width);
        if (hasOppositeSegment) Draw(oppositeBeam, oppositeStart, oppositeEnd, oppositeWidth);
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

    private void Draw(LineRenderer beam, Vector2 a, Vector2 b, float beamWidth)
    {
        if (beam == null) return;
        beam.enabled = true;
        beam.startWidth = beam.endWidth = beamWidth * (firing ? 1f : warningWidth);
        beam.startColor = beam.endColor = firing ? firingColor : warningColor;
        beam.SetPosition(0, new Vector3(a.x, a.y, transform.position.z));
        beam.SetPosition(1, new Vector3(b.x, b.y, transform.position.z));
    }

    public void Hide()
    {
        showing = firing = hasOppositeSegment = false;
        hitPlayers.Clear();
        if (roomBeam != null) roomBeam.enabled = false;
        if (oppositeBeam != null) oppositeBeam.enabled = false;
    }
    private void OnDisable() { Hide(); }
}

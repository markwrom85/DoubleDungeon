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
    private Portal.BeamPath path;
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
        Hide();
    }

    public void Configure(EnemyDungeonSide room, bool isLeft)
    { Hide(); side = room; onLeft = isLeft; portal = null; }

    private bool FindPortal() => (portal != null && portal.IsValidFor(side)) || Portal.TryFind(gameObject.scene, side, out portal);

    public Vector2 GetAimDirection(PlayerInput player)
    {
        if (player == null || !FindPortal()) return onLeft ? Vector2.right : Vector2.left;
        return portal.AimDirection(transform.position, player.transform.position, !side.IsValidTarget(player));
    }

    public Vector2 ClampDirection(Vector2 desired) => FindPortal()
        ? portal.ClampAim(transform.position, desired, side) : onLeft ? Vector2.right : Vector2.left;

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
        if (!FindPortal() || !portal.BuildBeam(side, transform.position, direction, diameter * widthRelativeToBody, out path))
        { Hide(); return false; }
        Portal.DrawBeam(path, roomBeam, oppositeBeam, firing ? firingColor : warningColor, firing ? 1f : warningWidth);
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

    private bool Touches(PlayerInput player, Portal.Segment segment)
    {
        Vector2 delta = segment.End - segment.Start;
        player.GetComponentsInChildren(false, colliders);
        foreach (Collider2D collider in colliders)
        {
            if (!collider.enabled || collider.isTrigger) continue;
            float along = delta.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - segment.Start, delta) / delta.sqrMagnitude) : 0f;
            Vector2 point = segment.Start + delta * along, contact = collider.ClosestPoint(point);
            if (segment.AllowsContact(contact) && (contact - point).sqrMagnitude <= segment.Width * segment.Width * 0.25f) return true;
        }
        return false;
    }

    public void Hide()
    {
        showing = firing = false; path = default; hitPlayers.Clear();
        if (roomBeam != null) roomBeam.enabled = false;
        if (oppositeBeam != null) oppositeBeam.enabled = false;
    }
    private void OnDisable() { Hide(); }
}

using System.Collections.Generic;
using UnityEngine;

// Optional callbacks let a movement controller keep its own state in sync after a generic transfer.
public interface IPortalUser
{
    bool CanUsePortal(Portal entrance);
    void OnPortalTravel(Portal.Transit transit);
}

// Add this to an object's movement root to opt in. Objects without it never teleport.
[DisallowMultipleComponent]
[DefaultExecutionOrder(1200)]
public sealed class PortalTraveller : MonoBehaviour
{
    public enum TravelMode { MovingObject = 0, ImobileObject = 1 }
    [SerializeField] private TravelMode mode;
    [SerializeField] private bool allowTravel = true;
    [SerializeField, Tooltip("Check Rigidbody or transform movement automatically. Disable for rays or custom movement.")]
    private bool automatic = true;
    [SerializeField] private bool scaleWithPortal = true;
    [SerializeField, Tooltip("Custom paths can resume their remaining movement from the exit themselves.")]
    private bool stopAtExit;

    private Rigidbody2D body;
    private Collider2D[] colliders;
    private Vector3 originalScale;
    private Vector2 sampledPosition;
    private Portal lastExit;
    private double lastTravelPhysicsTime = double.NegativeInfinity;
    private readonly List<IPortalUser> listeners = new List<IPortalUser>();
    private readonly Dictionary<Portal, Portal.OpeningPose> openings = new Dictionary<Portal, Portal.OpeningPose>();
    private readonly PortalBeam beam = new PortalBeam();
    public PortalBeam Beam => beam;
    internal bool StopAtExit => stopAtExit;
    public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
    public bool RecentlyTravelled => Time.fixedTimeAsDouble - lastTravelPhysicsTime <= Time.fixedDeltaTime * 1.1;

    // Cache movement and controller callbacks once; no enemy-type checks are needed.
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        originalScale = transform.localScale;
        foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            if (component is IPortalUser listener) listeners.Add(listener);
    }

    // Reused objects start with their original scale and no stale exit lock.
    private void OnEnable()
    {
        transform.localScale = originalScale;
        lastExit = null;
        lastTravelPhysicsTime = double.NegativeInfinity;
        ResetTracking();
    }

    private void FixedUpdate()
    {
        if (mode != TravelMode.MovingObject || !automatic || body == null || !body.simulated)
            return;

        if (body.linearVelocity.sqrMagnitude < 0.0001f) return;

        Vector2 from = body.position;
        Vector2 to = from + body.linearVelocity * Time.fixedDeltaTime;
        if (Portal.TryTravelAtBoundary(this, from, to, body.linearVelocity, out _))
        {
            ResetTracking();
            return;
        }

        if (Portal.TryTravel(this, from, to, body.linearVelocity, out _))
        {
            ResetTracking();
            return;
        }
    }

    // Restore the authored sprite and hide its routed sections when an attack or object is disabled.
    private void OnDisable()
    {
        beam.Hide();
    }

    // Release only the beam renderers owned by this traveller; their shared assets remain untouched.
    private void OnDestroy()
    {
        beam.Dispose();
    }

    // Ordinary physics and transform movement use the same crossing process as custom paths.
    private void LateUpdate()
    {
        if (mode == TravelMode.ImobileObject)
        {
            if (automatic) beam.UpdateSprite(this);
        }
        else
        {
            beam.Hide();
            if (automatic) CheckCrossing();
        }
    }

    // Beam attacks use the same routing whether their geometry comes from a sprite or their own controller.
    public bool TraceBeam(Vector2 origin, Vector2 direction, float range, float width,
        Collider2D sourceArea = null, Collider2D destinationArea = null)
    {
        if (mode != TravelMode.ImobileObject) { beam.Clear(); return false; }
        return beam.Trace(this, origin, direction, range, width, sourceArea, destinationArea);
    }

    // Check this object's permission plus optional state restrictions supplied by its controller.
    public bool CanUse(Portal entrance)
    {
        if (!isActiveAndEnabled || !allowTravel || (body != null && !body.simulated)) return false;
        foreach (IPortalUser listener in listeners)
            if (listener is Behaviour behaviour && !behaviour.isActiveAndEnabled || !listener.CanUsePortal(entrance)) return false;
        return true;
    }

    // Use after spawning or an unrelated teleport so it cannot be mistaken for portal movement.
    public void ResetTracking()
    {
        sampledPosition = Position;
        Portal.RememberOpenings(gameObject.scene, openings);
    }

    // Supply the opening seen at the start of this movement, even if another check ran earlier this frame.
    internal bool TryGetOpening(Portal portal, out Portal.OpeningPose pose) => openings.TryGetValue(portal, out pose);

    // Detect physics movement before a projectile collision/exit callback can retire it.
    public bool CheckCrossing()
    {
        if (mode != TravelMode.MovingObject) return false;
        Vector2 direction = body != null ? body.linearVelocity : Position - sampledPosition;
        bool crossed = Portal.TryTravel(this, sampledPosition, Position, direction, out _);
        ResetTracking();
        return crossed;
    }

    // Custom movement submits a desired position; the portal owns crossing detection and transfer.
    public bool MoveTo(Vector2 destination, Vector2 direction, out Portal.Transit transit)
    {
        if (mode != TravelMode.MovingObject) { transit = default; return false; }
        Vector2 from = Position;
        if (Portal.TryTravelAtBoundary(this, from, destination, direction, out transit))
        {
            ResetTracking();
            return true;
        }

        SetPosition(destination);
        bool crossed = Portal.TryTravel(this, from, destination, direction, out transit);
        ResetTracking();
        return crossed;
    }

    // Keep the exit locked until the whole object has moved back inside its new view.
    internal bool IsExitBlocked(Portal portal)
    {
        if (lastExit == null) return false;
        float radius = 0f;
        foreach (Collider2D collider in colliders)
            if (collider != null && collider.enabled)
                radius = Mathf.Max(radius, collider.bounds.extents.magnitude);
        if (!lastExit.isActiveAndEnabled || lastExit.SignedDistance(Position) > radius + 0.01f) lastExit = null;
        return lastExit == portal;
    }

    // Map movement and rotation, then notify controllers after the object has reached the exit.
    internal void ApplyTransit(Portal.Transit transit)
    {
        Vector2 facing = transit.Entrance.MapVector(transform.right);
        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        if (body != null)
        {
            body.linearVelocity = transit.Entrance.MapVector(body.linearVelocity);
            body.angularVelocity *= Mathf.Sign(Vector3.Cross(transit.Entrance.MapVector(Vector2.right),
                transit.Entrance.MapVector(Vector2.up)).z);
            body.rotation = angle;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (scaleWithPortal) transform.localScale *= transit.Scale;
        SetPosition(transit.Position);
        lastExit = transit.Exit;
        lastTravelPhysicsTime = Time.fixedTimeAsDouble;
        foreach (IPortalUser listener in listeners) listener.OnPortalTravel(transit);
    }

    // Set both the physics position and transform so later code sees the destination immediately.
    private void SetPosition(Vector2 position)
    {
        if (body != null) body.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}

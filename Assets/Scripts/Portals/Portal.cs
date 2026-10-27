using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Invisible passage. Objects opt in through PortalTraveller; the portal has no enemy or rendering code.
[RequireComponent(typeof(BoxCollider2D))]
[DefaultExecutionOrder(1100)]
public sealed class Portal : MonoBehaviour
{
    public enum CameraEdge { Left = -1, Right = 1 }
    [SerializeField] private Camera attachedCamera;
    [SerializeField] private CameraEdge cameraEdge = CameraEdge.Right;
    [SerializeField] private Portal linkedPortal;
    [SerializeField] private bool followCamera = true;
    [SerializeField] private float gameplayZ;
    [SerializeField, Min(0.01f)] private float triggerThickness = 0.1f;

    private static readonly List<Portal> portals = new List<Portal>();
    private BoxCollider2D opening;
    public Portal LinkedPortal => linkedPortal;
    public Camera AttachedCamera => attachedCamera;
    public BoxCollider2D Opening => opening != null ? opening : opening = GetComponent<BoxCollider2D>();
    public Vector2 Center => transform.TransformPoint(Opening.offset);
    public Vector2 Normal => transform.right * (float)cameraEdge;
    public Vector2 Up => transform.up;
    public float Height => Opening.size.y * transform.TransformVector(Vector3.up).magnitude;
    public float ExitScale => linkedPortal.Height / Height;

    // Details supplied to movement controllers after the portal has moved their object.
    public struct Transit
    {
        public Portal Entrance, Exit;
        public Vector2 Crossing, Position, Direction;
        public float Scale, Fraction;
    }

    // Each traveller remembers the opening it last observed, including camera movement within one frame.
    internal struct OpeningPose
    {
        public Vector2 Center, Normal, Up;
        public float Height;
    }

    // Reset scene-independent registration when a new play session starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { portals.Clear(); }

    // Fit the physical trigger before its first use, without treating initial placement as movement.
    private void Awake()
    {
        Opening.isTrigger = true;
        FitToCamera();
    }

    // Register this opening so any opted-in object can find it.
    private void OnEnable() { portals.Add(this); }

    // Remove closed or destroyed openings from crossing checks.
    private void OnDisable() { portals.Remove(this); }

    // Follow this camera after ordinary movement, independently of the linked camera.
    private void LateUpdate() { FitToCamera(); }

    // Record world values rather than child transforms that will move automatically with their camera parent.
    internal static void RememberOpenings(Scene scene, Dictionary<Portal, OpeningPose> poses)
    {
        poses.Clear();
        foreach (Portal portal in portals)
        {
            if (portal.gameObject.scene != scene) continue;
            portal.FitToCamera();
            poses[portal] = new OpeningPose { Center = portal.Center, Normal = portal.Normal, Up = portal.Up, Height = portal.Height };
        }
    }

    // Match this camera's assigned edge at the gameplay depth.
    public void FitToCamera()
    {
        if (followCamera && attachedCamera != null && attachedCamera.orthographic)
        {
            float depth = Mathf.Abs(gameplayZ - attachedCamera.transform.position.z);
            float edge = cameraEdge == CameraEdge.Right ? 1f : 0f;
            Vector3 bottom = attachedCamera.ViewportToWorldPoint(new Vector3(edge, 0f, depth));
            Vector3 top = attachedCamera.ViewportToWorldPoint(new Vector3(edge, 1f, depth));
            Vector3 center = (bottom + top) * 0.5f;
            center.z = gameplayZ;
            transform.SetPositionAndRotation(center, attachedCamera.transform.rotation);
            transform.localScale = Vector3.one;
            Vector3 scale = transform.lossyScale;
            Opening.size = new Vector2(triggerThickness / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                Vector3.Distance(bottom, top) / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
            Opening.isTrigger = true;
        }
    }

    // Require active, reciprocal openings in the same scene and in the 2D gameplay plane.
    public bool IsConnected => isActiveAndEnabled && Opening.isActiveAndEnabled && Opening.isTrigger
        && Height > 0.001f && Vector3.Dot(transform.forward, Vector3.forward) > 0.9999f
        && (!followCamera || (attachedCamera != null && attachedCamera.isActiveAndEnabled && attachedCamera.orthographic))
        && linkedPortal != null && linkedPortal != this && linkedPortal.linkedPortal == this
        && linkedPortal.gameObject.scene == gameObject.scene && linkedPortal.isActiveAndEnabled
        && linkedPortal.Opening.isActiveAndEnabled && linkedPortal.Opening.isTrigger && linkedPortal.Height > 0.001f
        && Vector3.Dot(linkedPortal.transform.forward, Vector3.forward) > 0.9999f
        && (!linkedPortal.followCamera || (linkedPortal.attachedCamera != null
            && linkedPortal.attachedCamera.isActiveAndEnabled && linkedPortal.attachedCamera.orthographic));

    // Let room-bound users verify that the cameras still show their original pair of areas.
    public bool IsValidFor(Collider2D source, Collider2D destination)
    {
        FitToCamera();
        if (linkedPortal != null) linkedPortal.FitToCamera();
        return IsConnected && source != null && destination != null
            && source.isActiveAndEnabled && destination.isActiveAndEnabled
            && source.gameObject.scene == gameObject.scene && destination.gameObject.scene == gameObject.scene
            && source.OverlapPoint(attachedCamera != null ? (Vector2)attachedCamera.transform.position : Center - Normal * 0.01f)
            && destination.OverlapPoint(linkedPortal.attachedCamera != null
                ? (Vector2)linkedPortal.attachedCamera.transform.position : linkedPortal.Center - linkedPortal.Normal * 0.01f);
    }

    // Find a connection between supplied areas without knowing anything about their enemies.
    public static bool TryFind(Scene scene, Collider2D source, Collider2D destination, out Portal portal)
    {
        foreach (Portal candidate in portals)
            if (candidate.gameObject.scene == scene && candidate.IsValidFor(source, destination))
            {
                portal = candidate;
                return true;
            }
        portal = null;
        return false;
    }

    // Positive distances are inside the source view.
    public float SignedDistance(Vector2 point) => -Vector2.Dot(point - Center, Normal);

    // Map world positions to corresponding positions at the other opening.
    public Vector2 MapPoint(Vector2 point) => linkedPortal.Center + MapVector(point - Center);

    // Preserve the relative direction and apparent speed between differently sized views.
    public Vector2 MapVector(Vector2 vector) => (-linkedPortal.Normal * Vector2.Dot(vector, Normal)
        + linkedPortal.Up * Vector2.Dot(vector, Up)) * ExitScale;

    // Restrict contact checks to the visible side of this opening.
    public bool ContainsVisible(Vector2 point) => SignedDistance(point) >= -0.0001f;

    // Route a permitted ray through the opening; its owner handles its range, damage, and drawing.
    public bool TryMapRay(PortalTraveller user, Vector2 origin, Vector2 direction, float range,
        out Vector2 entrance, out Vector2 exit, out Vector2 exitDirection)
    {
        entrance = exit = exitDirection = default;
        FitToCamera();
        if (linkedPortal != null) linkedPortal.FitToCamera();
        if (!IsConnected || user == null || user.gameObject.scene != gameObject.scene || !user.CanUse(this)) return false;
        direction.Normalize();
        float into = Vector2.Dot(direction, Normal);
        if (into <= 0.00001f) return false;
        float distance = SignedDistance(origin) / into;
        entrance = origin + direction * distance;
        if (distance < 0f || distance > range || Mathf.Abs(Vector2.Dot(entrance - Center, Up)) > Height * 0.5f) return false;
        exit = MapPoint(entrance);
        exitDirection = MapVector(direction).normalized;
        return true;
    }

    // Find the first permitted opening crossed by a movement, including fast shots and moving entrances.
    internal static bool TryTravel(PortalTraveller user, Vector2 from, Vector2 to, Vector2 direction, out Transit transit)
    {
        transit = default;
        Portal chosen = null;
        float first = float.PositiveInfinity;
        Vector2 crossing = default;
        foreach (Portal candidate in portals)
        {
            candidate.FitToCamera();
            if (candidate.linkedPortal != null) candidate.linkedPortal.FitToCamera();
            if (!candidate.IsConnected || candidate.gameObject.scene != user.gameObject.scene
                || !user.CanUse(candidate) || user.IsExitBlocked(candidate)) continue;
            bool previous = user.TryGetOpening(candidate, out OpeningPose pose);
            float before = previous ? -Vector2.Dot(from - pose.Center, pose.Normal) : candidate.SignedDistance(from);
            float after = candidate.SignedDistance(to);
            if (before <= 0f || after > 0f) continue;
            float fraction = Mathf.Clamp01(before / (before - after));
            Vector2 point = Vector2.Lerp(from, to, fraction);
            Vector2 center = previous ? Vector2.Lerp(pose.Center, candidate.Center, fraction) : candidate.Center;
            Vector2 up = previous ? Vector2.Lerp(pose.Up, candidate.Up, fraction).normalized : candidate.Up;
            float height = previous ? Mathf.Lerp(pose.Height, candidate.Height, fraction) : candidate.Height;
            float along = Vector2.Dot(point - center, up) / height;
            if (Mathf.Abs(along) > 0.5f || fraction >= first) continue;
            chosen = candidate;
            first = fraction;
            crossing = candidate.Center + candidate.Up * (along * candidate.Height);
        }
        if (chosen == null) return false;

        Vector2 destination = chosen.MapPoint(crossing);
        if (!user.StopAtExit) destination += chosen.MapVector((to - from) * (1f - first));
        destination -= chosen.linkedPortal.Normal * Mathf.Max(0f, 0.002f - chosen.linkedPortal.SignedDistance(destination));
        transit = new Transit
        {
            Entrance = chosen, Exit = chosen.linkedPortal, Crossing = Vector2.Lerp(from, to, first),
            Position = destination, Direction = chosen.MapVector(direction).normalized,
            Scale = chosen.ExitScale, Fraction = first
        };
        user.ApplyTransit(transit);
        return true;
    }
}

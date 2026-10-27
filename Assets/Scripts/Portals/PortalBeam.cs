using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared beam geometry. The emitter stays put; openings route the remaining length and width.
public sealed class PortalBeam
{
    public struct Segment
    {
        public Vector2 Start, End;
        public float Width;
        public Portal Entry, Exit;
        public Collider2D Area;

        // Damage uses the exact path and only the visible side of each opening.
        public bool Touches(Collider2D collider)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) return false;
            Vector2 delta = End - Start;
            float along = delta.sqrMagnitude > 0f
                ? Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - Start, delta) / delta.sqrMagnitude) : 0f;
            Vector2 point = Start + delta * along, contact = collider.ClosestPoint(point);
            return (Entry == null || Entry.ContainsVisible(contact)) && (Exit == null || Exit.ContainsVisible(contact))
                && (Area == null || Area.OverlapPoint(contact)) && (contact - point).sqrMagnitude <= Width * Width * 0.25f;
        }
    }

    private const int MaxSegments = 8;
    private readonly List<Segment> segments = new List<Segment>(2);
    private readonly List<SpriteRenderer> continuations = new List<SpriteRenderer>();
    private MaterialPropertyBlock properties;
    private SpriteRenderer source;
    private Vector3 authoredPosition, authoredScale;
    private Quaternion authoredRotation;
    public IReadOnlyList<Segment> Segments => segments;

    // Discard geometry without changing a manually controlled renderer.
    public void Clear() { segments.Clear(); }

    // Trace finite sprite beams or room-bounded rays through the same permission and mapping rules.
    public bool Trace(PortalTraveller user, Vector2 origin, Vector2 direction, float range, float width,
        Collider2D sourceArea = null, Collider2D destinationArea = null)
    {
        Clear();
        if (user == null || !user.isActiveAndEnabled || direction.sqrMagnitude < 0.0001f || range <= 0f
            || float.IsNaN(range) || width <= 0f) return false;
        direction.Normalize();
        Portal entry = null;
        Collider2D area = sourceArea, nextArea = destinationArea;
        for (int i = 0; i < MaxSegments; i++)
        {
            float limit = range;
            if (area != null)
            {
                Vector2 clipped = origin;
                if (!ClipEntry(area.bounds, ref clipped, direction)) break;
                float gap = Vector2.Distance(origin, clipped);
                if (gap > range) break;
                if (gap > 0.001f) entry = null;
                origin = clipped;
                range -= gap;
                limit = Mathf.Min(range, RoomExit(area.bounds, origin, direction));
            }
            if (float.IsInfinity(limit) || limit < 0.00001f) break;
            bool crossed = Portal.TryFindRay(user, origin, direction, limit, out Portal gate,
                out Vector2 hit, out Vector2 mapped, out Vector2 aim, area, nextArea);
            Vector2 end = crossed ? hit : origin + direction * limit;
            segments.Add(new Segment { Start = origin, End = end, Width = width, Entry = entry,
                Exit = crossed ? gate : null, Area = area });
            if (!crossed) break;
            range = (range - Vector2.Distance(origin, hit)) * gate.ExitScale;
            width *= gate.ExitScale;
            origin = mapped;
            direction = aim;
            entry = gate.LinkedPortal;
            Collider2D previousArea = area;
            area = nextArea;
            nextArea = previousArea;
        }
        return segments.Count > 0;
    }

    // Draw an existing LineRenderer from the same section used for contact checks.
    public void DrawLine(LineRenderer renderer, int index, float z, Color color, float widthFactor)
    {
        if (renderer == null) return;
        renderer.enabled = index < segments.Count;
        if (!renderer.enabled) return;
        Segment segment = segments[index];
        float width = segment.Width * widthFactor;
        VisibleEnds(segment, width, out Vector2 start, out Vector2 end);
        renderer.useWorldSpace = true;
        renderer.positionCount = 2;
        renderer.numCornerVertices = 0;
        renderer.startWidth = renderer.endWidth = width;
        renderer.startColor = renderer.endColor = color;
        renderer.SetPosition(0, new Vector3(start.x, start.y, z));
        renderer.SetPosition(1, new Vector3(end.x, end.y, z));
    }

    // Find our own sprite, including inactive attack children, then route it without moving its owner.
    public void UpdateSprite(PortalTraveller user)
    {
        SpriteRenderer sprite = source;
        if (sprite == null || !sprite.transform.IsChildOf(user.transform))
            sprite = user.GetComponentInChildren<SpriteRenderer>(true);
        if (source != sprite)
        {
            Hide();
            source = sprite;
            if (source != null)
            {
                authoredPosition = source.transform.localPosition;
                authoredRotation = source.transform.localRotation;
                authoredScale = source.transform.localScale;
            }
        }
        RestoreSprite();
        if (source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null)
        { Hide(); return; }
        Bounds bounds = source.sprite.bounds;
        Transform pose = source.transform;
        Vector3 start = pose.TransformPoint(new Vector3(bounds.min.x, bounds.center.y, bounds.center.z));
        Vector3 end = pose.TransformPoint(new Vector3(bounds.max.x, bounds.center.y, bounds.center.z));
        float width = pose.TransformVector(Vector3.up * bounds.size.y).magnitude;
        if (!Trace(user, start, end - start, Vector3.Distance(start, end), width)) { Hide(); return; }
        for (int i = 0; i < segments.Count; i++)
        {
            SpriteRenderer renderer = i == 0 ? source : GetContinuation(i - 1, user);
            if (i > 0) CopyAppearance(renderer);
            VisibleEnds(segments[i], segments[i].Width, out Vector2 a, out Vector2 b);
            PlaceSprite(renderer, a, b, segments[i].Width, start.z);
            renderer.gameObject.SetActive(true);
        }
        for (int i = segments.Count - 1; i < continuations.Count; i++) continuations[i].gameObject.SetActive(false);
    }

    // Reuse extra sections without creating material instances or portal-surface visuals.
    private SpriteRenderer GetContinuation(int index, PortalTraveller user)
    {
        if (index == continuations.Count)
        {
            GameObject section = new GameObject(user.name + " portal beam section");
            SceneManager.MoveGameObjectToScene(section, user.gameObject.scene);
            continuations.Add(section.AddComponent<SpriteRenderer>());
        }
        return continuations[index];
    }

    // Both rooms share the attack's existing assets, color and sorting.
    private void CopyAppearance(SpriteRenderer renderer)
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        renderer.gameObject.layer = source.gameObject.layer;
        renderer.sprite = source.sprite;
        renderer.sharedMaterials = source.sharedMaterials;
        renderer.color = source.color;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        source.GetPropertyBlock(properties);
        renderer.SetPropertyBlock(properties);
    }

    // Fit a rectangular sprite to a world-space section, preserving its sprite pivot.
    private static void PlaceSprite(SpriteRenderer renderer, Vector2 start, Vector2 end, float width, float z)
    {
        Bounds bounds = renderer.sprite.bounds;
        Vector2 delta = end - start;
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Vector3 scale = new Vector3(delta.magnitude / bounds.size.x, width / bounds.size.y, 1f);
        Vector3 center = (start + end) * 0.5f;
        center.z = z;
        Transform pose = renderer.transform;
        pose.SetPositionAndRotation(center - rotation * Vector3.Scale(bounds.center, scale), rotation);
        Vector3 parentScale = pose.parent != null ? pose.parent.lossyScale : Vector3.one;
        pose.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
    }

    // Cover the last camera pixel and diagonal beam edge; damage keeps its exact bounds.
    private static void VisibleEnds(Segment segment, float width, out Vector2 start, out Vector2 end)
    {
        Vector2 along = (segment.End - segment.Start).normalized;
        start = segment.Start - along * EdgePadding(segment.Entry, along, width);
        end = segment.End + along * EdgePadding(segment.Exit, along, width);
    }

    // Each opening uses its own camera, including when the cameras move or zoom independently.
    private static float EdgePadding(Portal gate, Vector2 along, float width)
    {
        if (gate == null) return 0f;
        return 0.5f * width * Mathf.Abs(Vector2.Dot(along, gate.Up))
            / Mathf.Max(0.0001f, Mathf.Abs(Vector2.Dot(along, gate.Normal)))
            + (gate.AttachedCamera != null && gate.AttachedCamera.pixelHeight > 0
                ? gate.Height / gate.AttachedCamera.pixelHeight : 0.002f);
    }

    // Restore before measuring again so clipping cannot shorten the next frame's attack.
    private void RestoreSprite()
    {
        if (source == null) return;
        source.transform.localPosition = authoredPosition;
        source.transform.localRotation = authoredRotation;
        source.transform.localScale = authoredScale;
    }

    // Stop pooled attacks immediately even when their emitter remains enabled.
    public void Hide()
    {
        RestoreSprite();
        foreach (SpriteRenderer renderer in continuations)
            if (renderer != null) renderer.gameObject.SetActive(false);
        Clear();
    }

    // Release owned sections on destruction, leaving shared sprites and materials intact.
    public void Dispose()
    {
        Hide();
        foreach (SpriteRenderer renderer in continuations)
            if (renderer != null) Object.Destroy(renderer.gameObject);
        continuations.Clear();
        source = null;
    }

    // Find where a ray leaves the supplied room's bounding box.
    private static float RoomExit(Bounds bounds, Vector2 point, Vector2 direction)
    {
        float x = Mathf.Abs(direction.x) > 0.00001f
            ? ((direction.x > 0f ? bounds.max.x : bounds.min.x) - point.x) / direction.x : float.PositiveInfinity;
        float y = Mathf.Abs(direction.y) > 0.00001f
            ? ((direction.y > 0f ? bounds.max.y : bounds.min.y) - point.y) / direction.y : float.PositiveInfinity;
        return Mathf.Max(0f, Mathf.Min(x, y));
    }

    // Trim offscreen rays to the destination room before rendering or applying damage.
    private static bool ClipEntry(Bounds bounds, ref Vector2 point, Vector2 direction)
    {
        float near = 0f, far = float.PositiveInfinity;
        for (int axis = 0; axis < 2; axis++)
        {
            if (Mathf.Abs(direction[axis]) < 0.00001f)
            {
                if (point[axis] < bounds.min[axis] || point[axis] > bounds.max[axis]) return false;
                continue;
            }
            float a = (bounds.min[axis] - point[axis]) / direction[axis];
            float b = (bounds.max[axis] - point[axis]) / direction[axis];
            near = Mathf.Max(near, Mathf.Min(a, b));
            far = Mathf.Min(far, Mathf.Max(a, b));
        }
        if (near > far || far < 0f) return false;
        point += direction * near;
        return true;
    }
}

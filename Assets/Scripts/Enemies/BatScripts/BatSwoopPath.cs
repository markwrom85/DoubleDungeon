using UnityEngine;

// A rounded pass through a locked target, sampled by distance so its speed stays even.
public sealed class BatSwoopPath
{
    private const int Samples = 64;
    private readonly Vector2[] points = new Vector2[7];
    private readonly float[] distances = new float[Samples + 1];
    public Vector2 Start => points[0];
    public Vector2 Target => points[3];
    public Vector2 End => points[6];
    public float Length => distances[Samples];

    public BatSwoopPath(Vector2 start, Vector2 target, Vector2 end, bool enteringFromDivider,
        Vector2 entryDirection = default)
    {
        float sign = end.x > start.x ? 1f : -1f;
        Vector2 inward = new Vector2(sign, 0f);
        float firstSpan = Mathf.Abs(target.x - start.x), lastSpan = Mathf.Abs(end.x - target.x);
        points[0] = start;
        Vector2 entry = entryDirection.sqrMagnitude > 0.0001f ? entryDirection.normalized : inward;
        points[1] = enteringFromDivider ? start + entry * firstSpan * 0.5f
            : new Vector2(start.x, Mathf.Lerp(start.y, target.y, 0.65f));
        points[2] = target - inward * firstSpan * 0.45f;
        points[3] = target;
        points[4] = target + inward * lastSpan * 0.45f;
        points[5] = end - inward * lastSpan * 0.5f;
        points[6] = end;
        Vector2 previous = start;
        for (int index = 1; index <= Samples; index++)
        {
            Vector2 next = Evaluate(index / (float)Samples);
            distances[index] = distances[index - 1] + Vector2.Distance(previous, next);
            previous = next;
        }
    }

    public Vector2 Evaluate(float progress)
    {
        float t = Mathf.Clamp01(progress) * 2f;
        int offset = t <= 1f ? 0 : 3;
        t = offset == 0 ? t : t - 1f;
        float u = 1f - t;
        return u * u * u * points[offset] + 3f * u * u * t * points[offset + 1]
            + 3f * u * t * t * points[offset + 2] + t * t * t * points[offset + 3];
    }

    public Vector2 AtDistance(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, Length);
        int low = 0, high = Samples;
        while (high - low > 1)
        {
            int middle = (low + high) / 2;
            if (distances[middle] < distance) low = middle;
            else high = middle;
        }
        float fraction = Mathf.InverseLerp(distances[low], distances[high], distance);
        return Evaluate((low + fraction) / Samples);
    }

    public Vector2 DirectionAtDistance(float distance)
    {
        float step = Mathf.Max(0.001f, Length * 0.001f);
        return (AtDistance(distance + step) - AtDistance(distance - step)).normalized;
    }
}

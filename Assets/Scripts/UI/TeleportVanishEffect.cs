using System.Collections;
using UnityEngine;

/// <summary>
/// Gives the player sprite a short vertical stretch-and-fade teleport effect.
/// </summary>
public sealed class TeleportVanishEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float phaseDuration = 0.2f;
    [SerializeField, Min(0f)] private float liftHeight = 0.4f;
    [SerializeField, Range(0.05f, 1f)] private float narrowScale = 0.35f;
    [SerializeField, Min(1f)] private float stretchScale = 1.7f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    public float PhaseDuration => phaseDuration;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    public IEnumerator Vanish(float duration)
    {
        yield return Animate(duration, appearing: false);
    }

    public IEnumerator Reappear(float duration)
    {
        yield return Animate(duration, appearing: true);
    }

    private IEnumerator Animate(float duration, bool appearing)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Apply(progress, appearing);
            yield return null;
        }

        Apply(1f, appearing);
    }

    private void Apply(float progress, bool appearing)
    {
        float amount = Mathf.SmoothStep(0f, 1f, progress);
        if (appearing) amount = 1f - amount;

        Vector3 scale = originalScale;
        scale.x *= Mathf.Lerp(1f, narrowScale, amount);
        scale.y *= Mathf.Lerp(1f, stretchScale, amount);
        transform.localScale = scale;
        transform.localPosition = originalPosition + Vector3.up * (liftHeight * amount);

        float alpha = 1f - amount;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            Color color = originalColors[i];
            color.a *= alpha;
            spriteRenderers[i].color = color;
        }
    }
}

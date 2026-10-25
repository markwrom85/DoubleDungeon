using UnityEngine;

/// <summary>
/// Applies the co-op player color to the pixel-art oval beneath the character.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerUnderglow : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float opacity = 0.7f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetPlayerId(int playerId)
    {
        Color color = playerId switch
        {
            1 => new Color(1f, 0.12f, 0.12f),
            2 => new Color(0.12f, 0.38f, 1f),
            3 => new Color(0.12f, 0.9f, 0.2f),
            4 => new Color(1f, 0.85f, 0.12f),
            _ => Color.white
        };
        color.a = opacity;
        spriteRenderer.color = color;
    }
}

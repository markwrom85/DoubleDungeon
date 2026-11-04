using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A warning circle becomes one damage pulse. Each eligible player is checked once.
public sealed class CauldronAreaEffect : MonoBehaviour
{
    [SerializeField] private LineRenderer boundary;
    [SerializeField] private LineRenderer countdown;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.35f;
    [SerializeField] private Color warningColor = new Color(0.65f, 1f, 0.15f, 0.8f);
    [SerializeField] private Color impactColor = new Color(0.8f, 1f, 0.25f, 1f);

    private readonly List<Collider2D> playerColliders = new List<Collider2D>(8);
    private EnemyDungeonSide side;
    private float radius, damage, warningStarted, warningDuration, detonatedAt;
    private bool configured;

    public bool HasDetonated { get; private set; }
    public float Radius => radius;
    public float PulseDuration => pulseDuration;

    public void ShowWarning(EnemyDungeonSide attackSide, float attackRadius, float attackDamage, float duration)
    {
        side = attackSide;
        radius = Mathf.Max(0.1f, attackRadius);
        damage = Mathf.Max(0f, attackDamage);
        warningStarted = Time.time;
        warningDuration = Mathf.Max(0.01f, duration);
        HasDetonated = false;
        configured = true;
        DrawCircle(boundary, radius, warningColor);
        DrawCircle(countdown, radius, warningColor);
    }

    private void Update()
    {
        if (!configured) return;
        if (!HasDetonated)
        {
            float remaining = 1f - Mathf.Clamp01((Time.time - warningStarted) / warningDuration);
            DrawCircle(countdown, radius * remaining, warningColor);
            return;
        }

        float progress = Mathf.Clamp01((Time.time - detonatedAt) / Mathf.Max(0.05f, pulseDuration));
        Color color = impactColor;
        color.a *= 1f - progress;
        DrawCircle(boundary, radius, color);
        DrawCircle(countdown, radius * progress, color);
        if (progress >= 1f) Destroy(gameObject);
    }

    public void Detonate()
    {
        if (!configured || HasDetonated || !isActiveAndEnabled) return;
        HasDetonated = true;
        detonatedAt = Time.time;
        DrawCircle(boundary, radius, impactColor);
        if (side == null || damage <= 0f) return;

        Vector2 center = transform.position;
        foreach (PlayerInput player in PlayerInput.all)
        {
            if (!side.IsValidTarget(player)) continue;
            PlayerInfo info = player.GetComponentInChildren<PlayerInfo>(true);
            if (info == null || info.CurrentHealth <= 0f) continue;

            player.GetComponentsInChildren(false, playerColliders);
            bool hasBody = false, overlaps = false;
            foreach (Collider2D collider in playerColliders)
            {
                if (!collider.enabled || collider.isTrigger) continue;
                hasBody = true;
                if ((collider.ClosestPoint(center) - center).sqrMagnitude <= radius * radius)
                    overlaps = true;
            }
            if (overlaps || (!hasBody && ((Vector2)player.transform.position - center).sqrMagnitude <= radius * radius))
                info.TakeDamage(damage);
        }
    }

    public void Cancel()
    {
        configured = false;
        Destroy(gameObject);
    }

    private static void DrawCircle(LineRenderer line, float circleRadius, Color color)
    {
        if (line == null) return;
        const int segments = 64;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startColor = line.endColor = color;
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            line.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * circleRadius);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

// Flight is visual: the locked ground circle, rather than contact in midair, deals damage.
public sealed class CauldronProjectile : MonoBehaviour
{
    [SerializeField] private CauldronAreaEffect areaPrefab;
    private CauldronAreaEffect area;
    private Vector3 start, destination;
    private float launchedAt, duration, height;
    private bool flying;

    public Vector3 Destination => destination;

    public void Launch(Vector3 landingPosition, EnemyDungeonSide side, float flightDuration,
        float arcHeight, float radius, float damage)
    {
        start = transform.position;
        destination = landingPosition;
        duration = Mathf.Max(0.05f, flightDuration);
        height = Mathf.Max(0.1f, arcHeight);
        launchedAt = Time.time;
        if (areaPrefab == null)
        {
            Debug.LogError("Cauldron projectile needs its area effect prefab.", this);
            Destroy(gameObject);
            return;
        }
        area = Instantiate(areaPrefab, destination, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(area.gameObject, gameObject.scene);
        area.ShowWarning(side, radius, damage, duration);
        flying = true;
    }

    private void Update()
    {
        if (!flying) return;
        float progress = Mathf.Clamp01((Time.time - launchedAt) / duration);
        transform.position = Vector3.Lerp(start, destination, progress)
            + Vector3.up * (4f * height * progress * (1f - progress));
        if (progress < 1f) return;
        flying = false;
        area?.Detonate();
        area = null; // The impact finishes independently of the projectile and its owner.
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        flying = false;
        if (area != null && !area.HasDetonated) area.Cancel();
        area = null;
    }
}

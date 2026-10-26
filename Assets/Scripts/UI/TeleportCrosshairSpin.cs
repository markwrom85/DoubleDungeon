using UnityEngine;

/// <summary>
/// Spins the teleport destination marker while it is visible.
/// </summary>
public sealed class TeleportCrosshairSpin : MonoBehaviour
{
    [SerializeField, Min(0f)] private float degreesPerSecond = 180f;

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}

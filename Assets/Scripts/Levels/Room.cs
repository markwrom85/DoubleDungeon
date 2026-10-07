using UnityEngine;

public class Room : MonoBehaviour
{
    public bool IsInCombat { get; private set; }

    public event System.Action<Room, bool> CombatStateChanged;

    private void SetCombatState(bool active)
    {
        if (IsInCombat == active) return;

        IsInCombat = active;
        CombatStateChanged?.Invoke(this, active);
    }
}
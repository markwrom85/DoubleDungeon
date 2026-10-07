using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private readonly HashSet<Room> activeRooms = new();

    public bool InCombat => activeRooms.Count > 0;

    public void RegisterRoom(Room room)
    {
        if (room == null) return;

        room.CombatStateChanged += OnRoomCombatStateChanged;

        if (room.IsInCombat)
            activeRooms.Add(room);
    }

    public void UnregisterRoom(Room room)
    {
        if (room == null) return;

        room.CombatStateChanged -= OnRoomCombatStateChanged;
        activeRooms.Remove(room);
    }

    private void OnRoomCombatStateChanged(Room room, bool isInCombat)
    {
        if (isInCombat)
            activeRooms.Add(room);
        else
            activeRooms.Remove(room);
    }
}
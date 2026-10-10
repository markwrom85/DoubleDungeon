using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class Room : MonoBehaviour
{
    private enum ScreenSide { None, Left, Right }

    [SerializeField] private CombatManager combatManager;
    private DungeonManager dungeonManager;
    [Header("Room Settings")]
    [SerializeField] private Collider2D roomBounds;
    [SerializeField] private Door[] doors;
    [SerializeField] private EnemySpawn[] enemySpawns;
    [SerializeField] private bool spawnOnLeftSide = true;
    [SerializeField] private bool spawnOnRightSide = true;

    private bool hasSpawnedOnLeft;
    private bool hasSpawnedOnRight;

    private void Start()
    {
        if (combatManager == null)
            combatManager = FindAnyObjectByType<CombatManager>();
        if (dungeonManager == null)
            dungeonManager = FindAnyObjectByType<DungeonManager>();
        if (combatManager == null) return;
        combatManager.AllEnemiesDefeated += OnAllEnemiesDefeated;
        if (combatManager.AreAllEnemiesDefeated)
            OnAllEnemiesDefeated();

        combatManager.CombatStarted += OnCombatStarted;
        if (combatManager.IsInCombat)
            OnCombatStarted();

        if (dungeonManager != null)
        {
            dungeonManager.RoomPairChanged += OnRoomPairChanged;
            OnRoomPairChanged(
                dungeonManager.leftCinemachineCamera,
                dungeonManager.rightCinemachineCamera);
        }
    }

    private void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.AllEnemiesDefeated -= OnAllEnemiesDefeated;
            combatManager.CombatStarted -= OnCombatStarted;
        }

        if (dungeonManager != null)
            dungeonManager.RoomPairChanged -= OnRoomPairChanged;
    }

    private void OnAllEnemiesDefeated()
    {
        foreach (Door door in doors)
        {
            door.isOpen = true;
            door.GetComponentInChildren<SpriteRenderer>().color = Color.green;
        }
    }

    private void OnCombatStarted()
    {
        foreach (Door door in doors)
        {
            door.isOpen = false;
            door.GetComponentInChildren<SpriteRenderer>().color = Color.red;
        }
    }

    private void OnRoomPairChanged(CinemachineCamera leftCamera, CinemachineCamera rightCamera)
    {
        ScreenSide visibleSide = GetVisibleSide(leftCamera, rightCamera);
        if (visibleSide == ScreenSide.None) return;

        if (visibleSide == ScreenSide.Left)
        {
            if (!spawnOnLeftSide || hasSpawnedOnLeft) return;
            hasSpawnedOnLeft = true;
        }
        else
        {
            if (!spawnOnRightSide || hasSpawnedOnRight) return;
            hasSpawnedOnRight = true;
        }

        SpawnEnemies();
    }

    private ScreenSide GetVisibleSide(
        CinemachineCamera leftCamera,
        CinemachineCamera rightCamera)
    {
        if (IsCameraInRoom(leftCamera)) return ScreenSide.Left;
        if (IsCameraInRoom(rightCamera)) return ScreenSide.Right;
        return ScreenSide.None;
    }

    private bool IsCameraInRoom(CinemachineCamera camera)
    {
        if (camera == null || roomBounds == null) return false;

        Transform roomParent = roomBounds.transform.parent;
        return (roomParent != null && camera.transform.IsChildOf(roomParent))
            || roomBounds.bounds.Contains(camera.transform.position);
    }

    private void SpawnEnemies()
    {
        foreach (EnemySpawn spawn in enemySpawns)
        {
            EnemyController enemy = Instantiate(spawn.enemyPrefab, spawn.transform.position, Quaternion.identity);
            combatManager.RegisterEnemy(enemy);
        }
    }
}
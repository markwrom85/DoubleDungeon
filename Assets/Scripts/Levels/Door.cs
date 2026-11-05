using UnityEngine;
using Unity.Cinemachine;

public class Door : MonoBehaviour
{
    public bool isOpen = false;
    [SerializeField] private CinemachineCamera newLeftCinemachineCamera, newRightCinemachineCamera;
    [SerializeField] private Transform playerLeftSpawnPoint, playerRightSpawnPoint;
    [SerializeField] private GameObject[] doorsToDisableAfterTransition;

    public void ChangeRooms(DungeonManager dungeonManager)
    {
        dungeonManager.ChangeRooms(newLeftCinemachineCamera, newRightCinemachineCamera, playerLeftSpawnPoint, playerRightSpawnPoint, doorsToDisableAfterTransition);
    }
}

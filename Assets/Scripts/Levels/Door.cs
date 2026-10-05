using UnityEngine;
using Unity.Cinemachine;

public class Door : MonoBehaviour
{
    public bool isOpen = false;
    private DungeonManager dungeonManager;
    [SerializeField] private CinemachineCamera newLeftCinemachineCamera, newRightCinemachineCamera;
    [SerializeField] private Transform newCenter, playerLeftSpawnPoint, playerRightSpawnPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dungeonManager = GameObject.Find("DungeonManager").GetComponent<DungeonManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        ArduinoJoystickPlayer player = other.GetComponentInParent<ArduinoJoystickPlayer>();
        if (player != null && isOpen)
        {
            dungeonManager.ChangeRooms(newLeftCinemachineCamera, newRightCinemachineCamera, newCenter, playerLeftSpawnPoint, playerRightSpawnPoint);
        }
    }
}

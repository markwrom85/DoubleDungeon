using UnityEngine;
using Unity.Cinemachine;

public class PlayerSwapSides : MonoBehaviour
{
    [SerializeField] private GameObject playerObj, crosshair;
    [SerializeField] private Transform leftCenter, rightCenter;
    [SerializeField] private bool isOnLeftSide;
    [SerializeField] private CinemachineTargetGroup leftTargetGroup, rightTargetGroup;
    [SerializeField] private Camera leftCamera, rightCamera;
    private ArduinoJoystickPlayer player;
    private bool isSwapping = false;

    private void Awake()
    {
        player = GetComponentInParent<ArduinoJoystickPlayer>();
        leftCenter = GameObject.Find("LeftCenter").transform;
        rightCenter = GameObject.Find("RightCenter").transform;

        leftTargetGroup = GameObject.Find("LeftPlayers").GetComponent<CinemachineTargetGroup>();
        rightTargetGroup = GameObject.Find("RightPlayers").GetComponent<CinemachineTargetGroup>();

        leftCamera = GameObject.Find("LeftCamera").GetComponent<Camera>();
        rightCamera = GameObject.Find("RightCamera").GetComponent<Camera>();
    }

    private void Update()
    {
        if (player == null || !player.SwitchTriggered) return;

        if (!isSwapping)
        {
            SwapSides();
            Debug.Log("Swapping sides");
        }
        else
        {
            EnableCharacter();
            Debug.Log("Enabling character");
        }
    }

    private void SwapSides()
    {
        isSwapping = true;
        playerObj.SetActive(false);
        crosshair.SetActive(true);
        if (isOnLeftSide)
        {
            player.SetMovementCamera(rightCamera);
            player.transform.position = rightCenter.position;
            leftTargetGroup.RemoveMember(player.transform);
            rightTargetGroup.AddMember(player.transform, 0.5f, 1f);
            isOnLeftSide = false;
        }
        else
        {
            player.SetMovementCamera(leftCamera);
            player.transform.position = leftCenter.position;
            rightTargetGroup.RemoveMember(player.transform);
            leftTargetGroup.AddMember(player.transform, 0.5f, 1f);
            isOnLeftSide = true;
        }
    }

    private void EnableCharacter()
    {
        playerObj.SetActive(true);
        crosshair.SetActive(false);
        isSwapping = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LeftDungeon"))
        {
            isOnLeftSide = true;
            Debug.Log("Player is on the left side");
        }
        else if (other.CompareTag("RightDungeon"))
        {
            isOnLeftSide = false;
            Debug.Log("Player is on the right side");
        }
    }
}

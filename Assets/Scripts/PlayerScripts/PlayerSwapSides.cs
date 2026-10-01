using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;

public class PlayerSwapSides : MonoBehaviour
{
    [SerializeField] private PlayerInfo playerInfo;
    [SerializeField] private GameObject playerObj, crosshair, aoeBurst;
    [SerializeField] private CardinalGun cardinalGun;
    [SerializeField] private Transform leftCenter, rightCenter;
    [SerializeField] private bool isOnLeftSide;
    [SerializeField] private CinemachineTargetGroup leftTargetGroup, rightTargetGroup;
    [SerializeField] private Camera leftCamera, rightCamera;
    
    private ArduinoJoystickPlayer player;
    private bool isSwapping = false, canSwap = true;
    private Coroutine swapTimer;

    private void Awake()
    {
        player = GetComponentInParent<ArduinoJoystickPlayer>();
        leftCenter = GameObject.Find("LeftCenter").transform;
        rightCenter = GameObject.Find("RightCenter").transform;

        leftTargetGroup = GameObject.Find("LeftPlayers").GetComponent<CinemachineTargetGroup>();
        rightTargetGroup = GameObject.Find("RightPlayers").GetComponent<CinemachineTargetGroup>();

        leftCamera = GameObject.Find("LeftCameraBrain").GetComponent<Camera>();
        rightCamera = GameObject.Find("RightCameraBrain").GetComponent<Camera>();
    }

    private void Update()
    {
        if (!canSwap) return;
        if (player == null || !player.SwitchTriggered) return;

        if (!isSwapping)
        {
            SwapSides();
        }
        else
        {
            EnableCharacter();
        }
    }

    private void SwapSides()
    {
        isSwapping = true;
        cardinalGun.canShoot = false;
        playerObj.SetActive(false);
        PlayerInput playerInput = player.GetComponentInParent<PlayerInput>();
        if (playerInput == null) return;

        Transform playerTarget = playerInput.transform;
        crosshair.SetActive(true);
        if (isOnLeftSide)
        {
            player.SetMovementCamera(rightCamera);
            MoveToSideCenter(rightCenter);
            leftTargetGroup.RemoveMember(playerTarget);
            rightTargetGroup.RemoveMember(playerTarget);
            rightTargetGroup.AddMember(playerTarget, 0.5f, 1f);
            isOnLeftSide = false;
        }
        else
        {
            player.SetMovementCamera(leftCamera);
            MoveToSideCenter(leftCenter);
            rightTargetGroup.RemoveMember(playerTarget);
            leftTargetGroup.RemoveMember(playerTarget);
            leftTargetGroup.AddMember(playerTarget, 0.5f, 1f);
            isOnLeftSide = true;
        }
        swapTimer = StartCoroutine(CompleteSwapAfterDelay());
    }

    public void RepositionForCurrentSide()
    {
        Transform sideCenter = isOnLeftSide ? leftCenter : rightCenter;
        MoveToSideCenter(sideCenter);
    }

    private void MoveToSideCenter(Transform sideCenter)
    {
        if (sideCenter == null) return;

        Vector3 position = player.transform.position;
        position.x = sideCenter.position.x;
        position.y = sideCenter.position.y;
        player.transform.position = position;
    }

    private void EnableCharacter()
    {
        if (swapTimer != null)
        {
            StopCoroutine(swapTimer);
            swapTimer = null;
        }
        playerObj.SetActive(true);
        crosshair.SetActive(false);
        isSwapping = false;
        cardinalGun.canShoot = true;
        StartCoroutine(AOEBurst());
    }

    private IEnumerator CompleteSwapAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, playerInfo.SwapDuration));
        if (isSwapping)
        {
            EnableCharacter();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LeftDungeon"))
        {
            isOnLeftSide = true;
        }
        else if (other.CompareTag("RightDungeon"))
        {
            isOnLeftSide = false;
        }
    }

    private IEnumerator AOEBurst()
    {
        aoeBurst.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        if (aoeBurst != null)
        {
            aoeBurst.SetActive(false);
        }
    }
}

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
    [SerializeField] private CinemachineTargetGroup leftTargetGroup, rightTargetGroup;
    [SerializeField] private Camera leftCamera, rightCamera;

    private ArduinoJoystickPlayer player;
    private bool canSwap = true;
    private Coroutine swapTimer;
    private bool IsOnLeftSide => player != null && player.MovementCamera == leftCamera;

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

        if (!player.isSwapping)
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
        player.isSwapping = true;
        cardinalGun.canShoot = false;
        playerObj.SetActive(false);
        PlayerInput playerInput = player.GetComponentInParent<PlayerInput>();
        if (playerInput == null) return;

        Transform playerTarget = playerInput.transform;
        crosshair.SetActive(true);
        if (IsOnLeftSide)
        {
            player.SetMovementCamera(rightCamera);
            MoveToSideCenter(rightCenter);
            leftTargetGroup.RemoveMember(playerTarget);
            rightTargetGroup.RemoveMember(playerTarget);
            rightTargetGroup.AddMember(playerTarget, 0.5f, 1f);
        }
        else
        {
            player.SetMovementCamera(leftCamera);
            MoveToSideCenter(leftCenter);
            rightTargetGroup.RemoveMember(playerTarget);
            leftTargetGroup.RemoveMember(playerTarget);
            leftTargetGroup.AddMember(playerTarget, 0.5f, 1f);
        }
        swapTimer = StartCoroutine(CompleteSwapAfterDelay());
    }

    public void RepositionForCurrentSide()
    {
        Transform sideCenter = IsOnLeftSide ? leftCenter : rightCenter;
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
        player.isSwapping = false;
        cardinalGun.canShoot = true;
        StartCoroutine(AOEBurst());
    }

    private IEnumerator CompleteSwapAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, playerInfo.SwapDuration));
        if (player.isSwapping)
        {
            EnableCharacter();
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

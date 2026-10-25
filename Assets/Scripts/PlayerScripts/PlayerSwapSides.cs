using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;

public class PlayerSwapSides : MonoBehaviour
{
    [SerializeField] private PlayerInfo playerInfo;
    [SerializeField] private GameObject playerObj, crosshair, aoeBurst;
    [SerializeField] private TeleportVanishEffect teleportVanishEffect;
    [SerializeField] private CardinalGun cardinalGun;
    [SerializeField] private Transform leftCenter, rightCenter;
    [SerializeField] private CinemachineTargetGroup leftTargetGroup, rightTargetGroup;
    [SerializeField] private Camera leftCamera, rightCamera;

    private ArduinoJoystickPlayer player;
    private bool canSwap = true;
    private bool IsOnLeftSide => player != null && player.MovementCamera == leftCamera;
    private bool panelSideInitialized;
    private bool panelIsOnLeft;

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
        SyncPanelSide();

        if (!canSwap || player == null || player.isSwapping || !player.SwitchTriggered) return;
        SwapSides();
    }

    private void SwapSides()
    {
        PlayerInput playerInput = player.GetComponentInParent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("Player has no parent PlayerInput component.", player);
            return;
        }

        bool wasOnLeftSide = IsOnLeftSide;
        float totalDuration = Mathf.Max(0f, playerInfo.SwapDuration);
        float phaseDuration = teleportVanishEffect != null
            ? Mathf.Min(teleportVanishEffect.PhaseDuration, totalDuration * 0.25f)
            : 0f;

        canSwap = false;
        player.isSwapping = true;
        cardinalGun.canShoot = false;
        StartCoroutine(SwapSidesRoutine(
            playerInput.transform,
            wasOnLeftSide,
            totalDuration,
            phaseDuration));
    }

    private IEnumerator SwapSidesRoutine(
        Transform playerTarget,
        bool wasOnLeftSide,
        float totalDuration,
        float phaseDuration)
    {
        if (teleportVanishEffect != null)
            yield return teleportVanishEffect.Vanish(phaseDuration);

        // Keep the existing targetability behavior while the player is between sides.
        playerObj.SetActive(false);
        TransferToOppositeSide(playerTarget, wasOnLeftSide);

        float destinationHold = Mathf.Max(0f, totalDuration - phaseDuration * 2f);
        if (destinationHold > 0f)
            yield return new WaitForSeconds(destinationHold);

        playerObj.SetActive(true);
        if (teleportVanishEffect != null)
            yield return teleportVanishEffect.Reappear(phaseDuration);

        CompleteSwap();
    }

    private void TransferToOppositeSide(Transform playerTarget, bool wasOnLeftSide)
    {
        if (wasOnLeftSide)
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
        crosshair.SetActive(true);
        playerInfo.SetPanelSide(!wasOnLeftSide);
        panelIsOnLeft = !wasOnLeftSide;
        panelSideInitialized = true;
    }

    private void SyncPanelSide()
    {
        if (player == null || player.MovementCamera == null) return;

        bool isOnLeft = IsOnLeftSide;
        if (panelSideInitialized && panelIsOnLeft == isOnLeft) return;

        playerInfo.SetPanelSide(isOnLeft);
        panelIsOnLeft = isOnLeft;
        panelSideInitialized = true;
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

    private void CompleteSwap()
    {
        playerObj.SetActive(true);
        crosshair.SetActive(false);
        player.isSwapping = false;
        cardinalGun.canShoot = true;
        StartCoroutine(AOEBurst());
        StartCoroutine(SwapCooldown());
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

    private IEnumerator SwapCooldown()
    {
        canSwap = false;
        yield return new WaitForSeconds(playerInfo.SwapCooldown);
        canSwap = true;
    }
}

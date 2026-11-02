using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrainLeft, cinemachineBrainRight;
    [SerializeField] private CinemachineCamera initialCameraLeft, initialCameraRight;
    [SerializeField] private CinemachineTargetGroup leftPlayers, rightPlayers;
    [SerializeField] private float standardBlendTime = 0.5f;
    private DungeonManager dungeonManager;

    void Start()
    {
        initialCameraLeft.Follow = leftPlayers.transform;
        initialCameraRight.Follow = rightPlayers.transform;

        StartCoroutine(InitializationCoroutine());
    }

    IEnumerator InitializationCoroutine()
    {
        dungeonManager = GetComponent<DungeonManager>();

        cinemachineBrainLeft.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        cinemachineBrainRight.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        yield return StartCoroutine(MoveAndWaitForCameraBlend(initialCameraLeft, initialCameraRight, null, null));

        cinemachineBrainLeft.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, standardBlendTime);
        cinemachineBrainRight.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, standardBlendTime);
    }

    public void MoveCamera(CinemachineCamera desiredCamLeft, CinemachineCamera desiredCamRight, CinemachineCamera oldCameraLeft, CinemachineCamera oldCameraRight)
    {
        // Changing priorities tells each CinemachineBrain to blend to the new room camera.
        if(oldCameraLeft != null && oldCameraRight != null)
        {
            oldCameraLeft.Priority = 0;
            oldCameraRight.Priority = 0;
        }

        desiredCamLeft.Priority = 10;
        desiredCamRight.Priority = 10;

        desiredCamLeft.Follow = leftPlayers.transform;
        desiredCamRight.Follow = rightPlayers.transform;

        // Set the output channels for the new cameras to ensure they are rendered on the correct display.
        desiredCamLeft.OutputChannel = OutputChannels.Channel01;
        desiredCamRight.OutputChannel = OutputChannels.Channel02;

        dungeonManager.leftCinemachineCamera = desiredCamLeft;
        dungeonManager.rightCinemachineCamera = desiredCamRight;
    }

    public IEnumerator MoveAndWaitForCameraBlend(CinemachineCamera desiredCamLeft, CinemachineCamera desiredCamRight, CinemachineCamera oldCameraLeft, CinemachineCamera oldCameraRight)
    {
        if (desiredCamLeft == null || desiredCamRight == null)
            yield break;

        MoveCamera(desiredCamLeft, desiredCamRight, oldCameraLeft, oldCameraRight);

        // Wait for Cinemachine to process the priority change before checking IsBlending.
        yield return null;
        yield return new WaitForEndOfFrame();

        // Keep the transition coroutine alive until both camera blends finish, with a timeout
        // as a safeguard if a brain is disabled or the blend cannot complete.
        float timeout = 2f;
        while ((cinemachineBrainLeft.IsBlending || cinemachineBrainRight.IsBlending) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
    }
}

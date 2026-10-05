using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrainLeft, cinemachineBrainRight;

    public void MoveCamera(CinemachineCamera desiredCamLeft, CinemachineCamera desiredCamRight, CinemachineCamera oldCameraLeft, CinemachineCamera oldCameraRight)
    {
        // Changing priorities tells each CinemachineBrain to blend to the new room camera.
        oldCameraLeft.Priority = 0;
        oldCameraRight.Priority = 0;
        desiredCamLeft.Priority = 10;
        desiredCamRight.Priority = 10;
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

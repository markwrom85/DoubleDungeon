using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
public void MoveCamera(CinemachineCamera desiredCam, CinemachineCamera oldCamera)
{
    oldCamera.Priority = 0;
    desiredCam.Priority = 10;
}

public IEnumerator MoveAndWaitForCameraBlend(CinemachineCamera desiredCam, CinemachineCamera oldCamera)
{
    if (desiredCam == null)
        yield break;

    MoveCamera(desiredCam, oldCamera);

    // let Cinemachine process priority change
    yield return null;
    yield return new WaitForEndOfFrame();

    // wait for blending to finish, but never hang forever
    float timeout = 2f;
    while (desiredCam.GetComponent<CinemachineBrain>().IsBlending && timeout > 0f)
    {
        timeout -= Time.unscaledDeltaTime;
        yield return null;
    }
}
}

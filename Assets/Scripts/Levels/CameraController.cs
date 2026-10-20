using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrainLeft, cinemachineBrainRight;
    [Header("Connected room views")]
    [SerializeField] private bool coordinateRoomViews = true;
    [SerializeField, Min(0f)] private float sharedFramingDamping = 0.3f;
    [SerializeField, Min(0f)] private float playerFramingPadding = 0.75f;
    private DungeonManager dungeonManager;

    internal bool CoordinateRoomViews => coordinateRoomViews && isActiveAndEnabled;
    internal float SharedFramingDamping => sharedFramingDamping;
    internal float PlayerFramingPadding => playerFramingPadding;

    private void Awake()
    {
        dungeonManager = GetComponent<DungeonManager>();
    }

    void Start()
    {
        if (dungeonManager != null)
            ConfigureRoomViews(dungeonManager.leftCinemachineCamera, dungeonManager.rightCinemachineCamera);
    }

    private void ConfigureRoomViews(CinemachineCamera left, CinemachineCamera right)
    {
        if (!coordinateRoomViews || left == null || right == null || left == right
            || cinemachineBrainLeft == null || cinemachineBrainRight == null) return;

        Collider2D leftBounds = left.GetComponent<CinemachineConfiner2D>()?.BoundingShape2D;
        Collider2D rightBounds = right.GetComponent<CinemachineConfiner2D>()?.BoundingShape2D;
        if (leftBounds == null || rightBounds == null || leftBounds == rightBounds) return;

        var pair = new RoomPairCameraFraming.Pair(this, left, right, leftBounds, rightBounds,
            cinemachineBrainLeft.OutputCamera, cinemachineBrainRight.OutputCamera);
        ConfigureView(left, pair, true);
        ConfigureView(right, pair, false);
    }

    private static void ConfigureView(CinemachineCamera camera, RoomPairCameraFraming.Pair pair, bool isLeft)
    {
        if (!camera.TryGetComponent(out RoomPairCameraFraming framing))
            framing = camera.gameObject.AddComponent<RoomPairCameraFraming>();
        framing.Configure(pair, isLeft);
        camera.PreviousStateIsValid = false;
    }

    public void MoveCamera(CinemachineCamera desiredCamLeft, CinemachineCamera desiredCamRight, CinemachineCamera oldCameraLeft, CinemachineCamera oldCameraRight)
    {
        ConfigureRoomViews(desiredCamLeft, desiredCamRight);

        // Changing priorities tells each CinemachineBrain to blend to the new room camera.
        oldCameraLeft.Priority = 0;
        oldCameraRight.Priority = 0;
        desiredCamLeft.Priority = 10;
        desiredCamRight.Priority = 10;

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

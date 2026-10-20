using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Both room borders project onto the divider without moving any gameplay geometry.
[DisallowMultipleComponent]
public class RoomPairCameraFraming : CinemachineExtension
{
    private Pair pair;
    private bool isLeft;

    internal void Configure(Pair roomPair, bool left)
    {
        pair = roomPair;
        isLeft = left;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || pair == null || !pair.IsValid) return;

        pair.UpdateFraming(deltaTime);
        Bounds bounds = (isLeft ? pair.LeftBounds : pair.RightBounds).bounds;
        Camera view = isLeft ? pair.LeftView : pair.RightView;
        float halfHeight = pair.HalfHeight * bounds.size.y;
        Vector3 position = state.GetFinalPosition();
        position.x = isLeft ? bounds.max.x - halfHeight * view.aspect
            : bounds.min.x + halfHeight * view.aspect;
        position.y = bounds.min.y + pair.Center * bounds.size.y;

        // Run after the individual follow/confiner stages, before Cinemachine blends the states.
        state.PositionCorrection += position - state.GetFinalPosition();
        state.RawOrientation = Quaternion.identity;
        state.OrientationCorrection = Quaternion.identity;
        state.Lens.Dutch = 0f;
        state.Lens.OrthographicSize = halfHeight;
    }

    internal sealed class Pair
    {
        private readonly CameraController owner;
        private readonly CinemachineCamera leftCamera, rightCamera;
        internal readonly Collider2D LeftBounds, RightBounds;
        internal readonly Camera LeftView, RightView;
        internal float Center { get; private set; } = 0.5f;
        internal float HalfHeight { get; private set; }
        private float centerVelocity, sizeVelocity;
        private int lastFrame = -1;
        private bool initialized;

        internal Pair(CameraController controller, CinemachineCamera left, CinemachineCamera right,
            Collider2D leftBounds, Collider2D rightBounds, Camera leftView, Camera rightView)
        {
            owner = controller;
            leftCamera = left;
            rightCamera = right;
            LeftBounds = leftBounds;
            RightBounds = rightBounds;
            LeftView = leftView;
            RightView = rightView;
        }

        internal bool IsValid => owner != null && owner.CoordinateRoomViews
            && leftCamera != null && rightCamera != null
            && LeftBounds != null && RightBounds != null
            && LeftBounds.isActiveAndEnabled && RightBounds.isActiveAndEnabled
            && LeftBounds.bounds.size.y > 0.001f && RightBounds.bounds.size.y > 0.001f
            && LeftView != null && RightView != null
            && LeftView.isActiveAndEnabled && RightView.isActiveAndEnabled
            && LeftView.orthographic && RightView.orthographic
            && LeftView.targetDisplay == RightView.targetDisplay
            && LeftView.pixelWidth > 0 && RightView.pixelWidth > 0
            && LeftView.pixelHeight > 0 && RightView.pixelHeight > 0
            && Mathf.Abs(LeftView.pixelRect.xMax - RightView.pixelRect.xMin) < 1f
            && Mathf.Abs(LeftView.pixelRect.yMin - RightView.pixelRect.yMin) < 1f
            && Mathf.Abs(LeftView.pixelRect.yMax - RightView.pixelRect.yMax) < 1f;

        internal void UpdateFraming(float deltaTime)
        {
            // Both virtual cameras consume exactly the same sample, regardless of update order.
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            Bounds left = LeftBounds.bounds, right = RightBounds.bounds;
            float targetSize = Mathf.Max(leftCamera.Lens.OrthographicSize / left.size.y,
                rightCamera.Lens.OrthographicSize / right.size.y);
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            foreach (PlayerInput player in PlayerInput.all)
            {
                if (player == null || !player.isActiveAndEnabled) continue;
                Vector2 position = player.transform.position;
                bool inLeft = Contains(left, position);
                if (!inLeft && !Contains(right, position)) continue;
                Bounds room = inLeft ? left : right;
                Camera view = inLeft ? LeftView : RightView;
                float padding = owner.PlayerFramingPadding;
                low = Mathf.Min(low, (position.y - padding - room.min.y) / room.size.y);
                high = Mathf.Max(high, (position.y + padding - room.min.y) / room.size.y);
                float distanceFromDivider = inLeft ? room.max.x - position.x : position.x - room.min.x;
                targetSize = Mathf.Max(targetSize,
                    (distanceFromDivider + padding) / (2f * view.aspect * room.size.y));
            }

            // Retain the outgoing framing while a room transition moves its players elsewhere.
            bool hasPlayers = !float.IsPositiveInfinity(low);
            if (initialized && !hasPlayers) return;
            float targetCenter = hasPlayers ? (low + high) * 0.5f : 0.5f;
            if (hasPlayers) targetSize = Mathf.Max(targetSize, (high - low) * 0.5f);
            targetSize = Mathf.Max(0.001f, targetSize);
            targetCenter = targetSize < 0.5f ? Mathf.Clamp(targetCenter, targetSize, 1f - targetSize) : 0.5f;

            float damping = owner.SharedFramingDamping;
            if (!initialized || deltaTime < 0f || damping <= 0f)
            {
                Center = targetCenter;
                HalfHeight = targetSize;
                centerVelocity = sizeVelocity = 0f;
                initialized = true;
                return;
            }
            Center = Mathf.SmoothDamp(Center, targetCenter, ref centerVelocity, damping,
                Mathf.Infinity, deltaTime);
            HalfHeight = Mathf.SmoothDamp(HalfHeight, targetSize, ref sizeVelocity, damping,
                Mathf.Infinity, deltaTime);
        }

        private static bool Contains(Bounds bounds, Vector2 point) => point.x >= bounds.min.x
            && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y;
    }
}

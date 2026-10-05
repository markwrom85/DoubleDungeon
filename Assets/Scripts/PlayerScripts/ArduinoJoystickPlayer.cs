using System;
using UnityEngine;
using UnityEngine.InputSystem;

// All controller types feed PlayerInput actions; Rigidbody2D applies movement.
[RequireComponent(typeof(Rigidbody2D))]
public class ArduinoJoystickPlayer : MonoBehaviour
{
    [SerializeField] private PlayerInfo playerInfo;
    public bool mouseMovement = true;

    [Header("Shooting")]
    public CardinalGun gun;

    [SerializeField] private Camera movementCamera;
    [SerializeField] private PlayerInput playerInput;

    private Rigidbody2D body;
    private Vector2 movementDirection;
    private DesktopMovementInput desktopInput;
    private string activeInput = "Idle";

    private Rect OverlayRect => new Rect(
        12,
        12,
        Mathf.Min(700, Screen.width - 24),
        215
    );

    public bool SwitchTriggered =>
        desktopInput != null && desktopInput.SwitchTriggered;

    private Texture2D squareTexture;
    private Sprite squareSprite;

    public bool isPlayable = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (gun == null)
            gun = GetComponentInChildren<CardinalGun>();

        squareTexture = new Texture2D(1, 1);
        squareTexture.SetPixel(0, 0, Color.white);
        squareTexture.Apply();

        squareSprite = Sprite.Create(
            squareTexture,
            new Rect(0, 0, 1, 1),
            Vector2.one * 0.5f,
            1
        );
    }

    private void OnEnable()
    {
        try
        {
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            if (playerInput == null)
                throw new InvalidOperationException(
                    "Add a PlayerInput component to the player prefab."
                );

            if (desktopInput == null)
                desktopInput = new DesktopMovementInput(playerInput);

            desktopInput.Enable();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Player input could not start. Check the Player actions in InputSystem_Actions: "
                + exception.Message
                + " Fix the asset, then stop and restart Play.",
                this
            );

            enabled = false;
            return;
        }
    }

    private void Update()
    {
        movementDirection = Vector2.zero;

        if (!isPlayable || desktopInput == null || !Application.isFocused)
        {
            activeInput = "Idle";
            return;
        }

        float step = Mathf.Max(0, playerInfo.MoveSpeed) * Time.deltaTime;

        if (!desktopInput.TryGetMovement(
            transform.position,
            movementCamera,
            step,
            OverlayRect,
            mouseMovement,
            out Vector2 direction,
            out activeInput))
        {
            direction = Vector2.zero;
        }

        movementDirection = Vector2.ClampMagnitude(direction, 1);

        // Update the player's sprite based on movement direction.
        playerInfo.SetDirection(movementDirection);

        if (gun != null)
            gun.Tick(direction, desktopInput.FireHeld);
    }

    private void FixedUpdate()
    {
        if (!isPlayable)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = movementDirection * playerInfo.MoveSpeed;
        // Camera-bounds movement restriction disabled. Level colliders should restrict movement instead.
        // Camera camera = movementCamera;
        // if (camera != null && camera.orthographic)
        // {
        //     float depth = Mathf.Abs(transform.position.z - camera.transform.position.z);
        //     Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        //     Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
        //     Collider2D collider = GetComponentInChildren<Collider2D>(true);
        //     Vector2 extents = collider != null ? collider.bounds.extents : Vector2.zero;
        //     Vector2 colliderOffset = collider != null
        //         ? (Vector2)collider.bounds.center - body.position
        //         : Vector2.zero;
        //
        //     Vector3 position = body.position;
        //
        //     position.x = Mathf.Clamp(
        //         position.x,
        //         bottomLeft.x + extents.x - colliderOffset.x,
        //         topRight.x - extents.x - colliderOffset.x);
        //
        //     position.y = Mathf.Clamp(
        //         position.y,
        //         bottomLeft.y + extents.y - colliderOffset.y,
        //         topRight.y - extents.y - colliderOffset.y);
        //
        //     body.position = position;
        // }
    }

    public void SetMovementCamera(Camera camera)
    {
        movementCamera = camera;
    }

    private void OnDisable()
    {
        desktopInput?.Disable();

        activeInput = "Idle";
        movementDirection = Vector2.zero;

        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        desktopInput?.Dispose();

        if (squareSprite != null)
            Destroy(squareSprite);

        if (squareTexture != null)
            Destroy(squareTexture);
    }
}

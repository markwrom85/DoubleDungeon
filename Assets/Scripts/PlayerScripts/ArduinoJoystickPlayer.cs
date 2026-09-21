using System;
using UnityEngine;
using UnityEngine.InputSystem;

// All controller types feed PlayerInput actions; Rigidbody2D applies movement.
[RequireComponent(typeof(Rigidbody2D))]
public class ArduinoJoystickPlayer : MonoBehaviour
{
    public float speed = 4f;
    public bool mouseMovement = true;
    [Header("Shooting")]
    public CardinalGun gun;

    [SerializeField] private PlayerInput playerInput;
    private Rigidbody2D body;
    private Vector2 movementDirection;
    private DesktopMovementInput desktopInput;
    private string activeInput = "Idle";
    private Rect OverlayRect => new Rect(12, 12, Mathf.Min(700, Screen.width - 24), 215);

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
        if (gun == null) gun = GetComponentInChildren<CardinalGun>();
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
        if (renderer.sprite != null) return;
        squareTexture = new Texture2D(1, 1);
        squareTexture.SetPixel(0, 0, Color.white);
        squareTexture.Apply();
        squareSprite = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        renderer.sprite = squareSprite;
        renderer.color = new Color(0.2f, 0.9f, 0.65f);
    }

    private void OnEnable()
    {
        try
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
                throw new InvalidOperationException("Add a PlayerInput component to the player prefab.");
            if (desktopInput == null) desktopInput = new DesktopMovementInput(playerInput);
            desktopInput.Enable();
        }
        catch (Exception exception)
        {
            Debug.LogError("Player input could not start. Check the Player actions in InputSystem_Actions: "
                + exception.Message + " Fix the asset, then stop and restart Play.", this);
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
        Camera camera = Camera.main;
        float step = Mathf.Max(0, speed) * Time.deltaTime;
        desktopInput.TryGetMovement(transform.position, camera, step, OverlayRect, mouseMovement,
            out Vector2 direction, out activeInput);
        movementDirection = Vector2.ClampMagnitude(direction, 1);
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

        body.linearVelocity = movementDirection * speed;
        Camera camera = Camera.main;
        if (camera != null && camera.orthographic)
        {
            float height = Mathf.Max(0, camera.orthographicSize - 0.5f);
            float width = Mathf.Max(0, camera.orthographicSize * camera.aspect - 0.5f);
            Vector3 position = body.position;
            position.x = Mathf.Clamp(position.x, camera.transform.position.x - width, camera.transform.position.x + width);
            position.y = Mathf.Clamp(position.y, camera.transform.position.y - height, camera.transform.position.y + height);
            body.position = position;
        }
    }

    private void OnDisable()
    {
        desktopInput?.Disable();
        activeInput = "Idle";
        movementDirection = Vector2.zero;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        desktopInput?.Dispose();
        if (squareSprite != null) Destroy(squareSprite);
        if (squareTexture != null) Destroy(squareTexture);
    }
}

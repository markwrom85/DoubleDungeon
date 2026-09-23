using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Owned by the player; bindings resolve again when devices connect or disconnect.
public sealed class DesktopMovementInput : IDisposable
{
    private readonly PlayerInput playerInput;
    private readonly InputActionAsset actions;
    private readonly InputAction move;
    private readonly InputAction attack;
    public bool FireHeld => attack.IsPressed();

    public DesktopMovementInput(PlayerInput owner)
    {
        playerInput = owner ?? throw new ArgumentNullException(nameof(owner));
        var source = playerInput.actions;
        if (source == null)
            throw new InvalidOperationException("Assign InputSystem_Actions to the PlayerInput component.");
        source.FindAction("Player/Move", true);
        source.FindAction("Player/Attack", true);
        actions = source;
        move = actions.FindAction("Player/Move", true);
        attack = actions.FindAction("Player/Attack", true);
    }

    public void Enable() { playerInput.ActivateInput(); }
    public void Disable() { playerInput.DeactivateInput(); }
    public void Dispose() { }

    // Return true even at the mouse target, so Arduino input cannot move it away.
    public bool TryGetMovement(Vector3 position, Camera camera, float step, Rect overlay,
        bool allowMouse, out Vector2 direction, out string source)
    {
        direction = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);
        source = "Idle";
        if (direction.sqrMagnitude > 0)
        {
            source = move.activeControl?.device?.displayName ?? "Keyboard / controller";
            return true;
        }

        return true;
    }
}

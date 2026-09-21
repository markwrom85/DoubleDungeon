using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Sequential)]
public struct ArduinoInputState : IInputStateTypeInfo
{
    public FourCC format => new FourCC('A', 'R', 'D', 'U');
    // The manager already calibrates and applies the dead zone; do not apply it twice.
    [InputControl(layout = "Vector2")]
    public Vector2 move;
    [InputControl(layout = "Button", format = "FLT")]
    public float fire;
}

[InputControlLayout(stateType = typeof(ArduinoInputState), displayName = "Arduino Controller")]
public class ArduinoInputDevice : InputDevice
{
    public Vector2Control move { get; private set; }
    public ButtonControl fire { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        move = GetChildControl<Vector2Control>("move");
        fire = GetChildControl<ButtonControl>("fire");
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Register() { InputSystem.RegisterLayout<ArduinoInputDevice>(); }
}

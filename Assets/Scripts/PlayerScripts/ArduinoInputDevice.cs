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
    [InputControl(layout = "Button", format = "FLT")]
    public float button2;
    [InputControl(layout = "Button", format = "FLT")]
    public float switchSide;
}

[InputControlLayout(stateType = typeof(ArduinoInputState), displayName = "Arduino Controller")]
public class ArduinoInputDevice : InputDevice
{
    public Vector2Control move { get; private set; }
    public ButtonControl fire { get; private set; }
    public ButtonControl button2 { get; private set; }
    public ButtonControl switchSide { get; private set; }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        move = GetChildControl<Vector2Control>("move");
        fire = GetChildControl<ButtonControl>("fire");
        button2 = GetChildControl<ButtonControl>("button2");
        switchSide = GetChildControl<ButtonControl>("switchSide");
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Register() { InputSystem.RegisterLayout<ArduinoInputDevice>(); }
}

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEditor;
using UnityEngine.InputSystem.Controls;
using System;

[StructLayout(LayoutKind.Explicit, Size = 8)]
struct ATSGamepadHIDInputReport : IInputStateTypeInfo
{
    public FourCC format => new FourCC('H', 'I', 'D');

    [InputControl(name = "trigger", layout = "Button", displayName = "Trigger", bit = 0)]
    [FieldOffset(1)] public byte buttons1;

    [InputControl(layout = "Vector2", name = "position", displayName = "Position", usage = "Point", dontReset = true, format = "VC2S")]
    [InputControl(layout = "Axis", name = "position/x", dontReset = true, format = "SHRT", offset = 0, parameters = "normalize = true, normalizeMin = -0.6246891, normalizeMax = 0.6247196, normalizeZero = 1.525879E-05")]
    [InputControl(layout = "Axis", name = "position/y", dontReset = true, format = "SHRT", offset = 2, parameters = "normalize = true, normalizeMin = -0.6246891, normalizeMax = 0.6247196, normalizeZero = 1.525879E-05")]
    [FieldOffset(2)] public Vector2 position;

    [InputControl(layout = "Analog", name = "rz", dontReset = true, parameters = "normalize = true, normalizeMin = -0.1249866, normalizeMax = 0.1250172, normalizeZero = 1.525879E-05")]
    [FieldOffset(6)] public Int16 rz;

    [InputControl(name = "fire", layout = "Button", displayName = "Fire", bit = 0)]
    [FieldOffset(8)] public byte buttons2;
}

[InputControlLayout(stateType = typeof(ATSGamepadHIDInputReport))]
#if UNITY_EDITOR
[InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
public class ATSGamepadHID : InputDevice
{
    public ButtonControl trigger { get; protected set; }
    public ButtonControl fire { get; protected set; }

    public Vector2Control position { get; protected set; }

    static ATSGamepadHID() {
        InputSystem.RegisterLayout<ATSGamepadHID>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithCapability("vendorId", 0x1915)
                .WithCapability("productId", 0xEEEE));
    }

    // In the Player, to trigger the calling of the static constructor,
    // create an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoadMethod]
    static void Init() { }
}

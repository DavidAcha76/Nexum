// SimpleJoystickInput.cs
using UnityEngine;

public class SimpleJoystickInput : MonoBehaviour, IInputSource
{
    [Header("UI")]
    public SimpleJoystick joystick;            // arrástralo aquí
    public UISprintHoldButton sprintButton;    // arrástralo aquí (el botón de sprint)

    public Vector2 Move => joystick ? joystick.Direction : Vector2.zero;
    public bool Sprint => sprintButton && sprintButton.IsHeld;
}

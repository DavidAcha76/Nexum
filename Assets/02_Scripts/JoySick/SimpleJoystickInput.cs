// SimpleJoystickInput.cs
using UnityEngine;

public class SimpleJoystickInput : MonoBehaviour, IInputSource
{
    [Header("UI")]
    public SimpleJoystick joystick;            // arr�stralo aqu�
    public UISprintHoldButton sprintButton;    // arr�stralo aqu� (el bot�n de sprint)

    public Vector2 Move => joystick ? joystick.Direction : Vector2.zero;
    public bool Sprint => sprintButton && sprintButton.IsHeld;
}

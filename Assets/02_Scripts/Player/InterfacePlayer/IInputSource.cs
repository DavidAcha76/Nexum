using UnityEngine;

public interface IInputSource
{
    Vector2 Move { get; }   // (x,y) en plano XZ
    bool Sprint { get; }    // mantener pulsado
}

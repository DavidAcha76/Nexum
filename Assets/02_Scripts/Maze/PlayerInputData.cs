using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 move;     // eje -1..1
    public bool dash;        // botón dash
    public bool ultimate;    // botón ultimate
}

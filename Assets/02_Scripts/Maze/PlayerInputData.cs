// PlayerInputData.cs
// Paquete de input por tick.

using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 move;
    public bool jump;
}

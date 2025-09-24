using Fusion;
using UnityEngine;

/// Activa cámara + audio SOLO para el jugador local.
/// Evita tener listeners en los jugadores remotos.
[RequireComponent(typeof(NetworkObject))]
public class PlayerLocalCamera : NetworkBehaviour
{
    [Header("Referencias del propio prefab")]
    public Camera playerCamera;
    public AudioListener playerAudio;

    public override void Spawned()
    {
        bool isLocal = Object.HasInputAuthority; // este runner controla este player
        if (playerCamera) playerCamera.enabled = isLocal;
        if (playerAudio) playerAudio.enabled = isLocal;
    }
}

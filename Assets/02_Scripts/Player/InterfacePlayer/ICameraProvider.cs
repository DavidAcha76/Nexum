using UnityEngine;

public interface ICameraProvider
{
    /// <summary>
    /// Devuelve forward/right en el plano XZ para movimiento relativo a cámara.
    /// Debe normalizar y poner y=0 en ambos vectores.
    /// </summary>
    bool TryGetPlanarVectors(out Vector3 forwardXZ, out Vector3 rightXZ);
}

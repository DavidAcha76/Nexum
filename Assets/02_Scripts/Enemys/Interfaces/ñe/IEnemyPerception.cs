// IEnemyPerception.cs
using UnityEngine;
public interface IEnemyPerception
{
    /// Devuelve el objetivo si está visible / válido; null si no.
    Transform AcquireTarget();
}

// IEnemyPerception.cs
using UnityEngine;
public interface IEnemyPerception
{
    /// Devuelve el objetivo si est� visible / v�lido; null si no.
    Transform AcquireTarget();
}

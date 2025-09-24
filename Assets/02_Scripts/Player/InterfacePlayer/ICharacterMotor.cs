using UnityEngine;

public interface ICharacterMotor
{
    /// <summary>Velocidad actual (se puede leer/escribir).</summary>
    Vector3 Velocity { get; set; }

    /// <summary>Aplica una rotación (puede ser slerp interno o directo).</summary>
    void MoveRotation(Quaternion targetRotation, float slerpFactor);

    /// <summary>Retorna el componente base (ej. Rigidbody) si necesitas acceder.</summary>
    Component RawComponent { get; }
}

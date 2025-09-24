// IEnemyMotor.cs
using UnityEngine;
public interface IEnemyMotor
{
    Vector3 Velocity { get; set; }
    void MoveTowards(Vector3 worldPos, float speed, float dt);
    void MoveAwayFrom(Vector3 worldPos, float speed, float dt);
    void FaceTowards(Vector3 worldPos, float turnSpeed, float dt);
    Component Raw { get; }
}

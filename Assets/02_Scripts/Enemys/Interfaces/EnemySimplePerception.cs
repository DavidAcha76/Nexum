using UnityEngine;

public class EnemySimplePerception : MonoBehaviour, IEnemyPerception
{
    private Transform player;

    void Awake()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go) player = go.transform;
    }

    public Transform AcquireTarget()
    {
        return player;
    }
}

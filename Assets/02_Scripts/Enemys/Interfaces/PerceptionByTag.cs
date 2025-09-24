// PerceptionByTag.cs
using UnityEngine;

public class PerceptionByTag : MonoBehaviour, IEnemyPerception
{
    public string targetTag = "Player";
    public float detectionRange = 15f;

    public Transform AcquireTarget()
    {
        var players = GameObject.FindGameObjectsWithTag(targetTag);
        Transform best = null; float bestSqr = detectionRange * detectionRange;

        foreach (var go in players)
        {
            if (!go) continue;
            float d2 = (go.transform.position - transform.position).sqrMagnitude;
            if (d2 <= bestSqr) { bestSqr = d2; best = go.transform; }
        }
        return best;
    }
}

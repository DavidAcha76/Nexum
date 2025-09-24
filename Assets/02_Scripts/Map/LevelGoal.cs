using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // ¿Entró el player?
        var pm = other.GetComponentInParent<PlayerController>() ?? other.GetComponent<PlayerController>();
        if (!pm) return;

        Debug.Log("[LevelGoal] Player reached goal");
        RunManager.Instance?.OnReachGoal();
    }
}

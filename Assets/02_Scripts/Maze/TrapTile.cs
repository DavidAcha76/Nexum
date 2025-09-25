// TrapTile.cs
// Efecto simple al pisar. Si lo quieres sincronizado por red, conviértelo en NetworkBehaviour y maneja estados en Host.

using UnityEngine;

public class TrapTile : MonoBehaviour
{
    public bool consumeOnTrigger = true;
    public float knockUpForce = 4f;
    public bool debugLog = false;

    private bool _consumed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;

        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForce(Vector3.up * knockUpForce, ForceMode.VelocityChange);
        }

        if (debugLog) Debug.Log($"TrapTile activada por {other.name}");

        if (consumeOnTrigger)
        {
            _consumed = true;
            gameObject.SetActive(false);
        }
    }
}

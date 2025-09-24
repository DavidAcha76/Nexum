using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TrapTile — Igual que un piso normal hasta que lo pisa el player.
/// - Se activa al entrar un collider con tag "Player".
/// - Si el player tiene Rigidbody, aplica un empujón vertical (knock-up).
/// - Dispara un UnityEvent y un SendMessage opcional para que tu controller haga lo que quiera (daño, slow, etc.).
/// - Puede auto-consumirse (desactivarse) al activarse.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrapTile : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Solo reacciona a objetos con tag 'Player'")]
    public string playerTag = "Player";
    [Tooltip("Desactivar objeto de trampa al activarse (consumible)")]
    public bool consumeOnTrigger = true;

    [Header("Efecto simple (opcional)")]
    [Tooltip("Fuerza vertical si el Player tiene Rigidbody")]
    public float knockUpForce = 4f;

    [Header("Hooks")]
    public UnityEvent onTriggered; // conéctalo a tu sistema de daño/slow/sonido/FX
    [Tooltip("Enviar mensaje 'OnTrapTriggered' al Player si existe ese método")]
    public bool sendMessageToPlayer = true;

    [Header("Debug")]
    public bool debugLog = false;

    bool _fired = false;

    void Reset()
    {
        // Asegura trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        _fired = true;

        if (debugLog) Debug.Log($"[TrapTile] Triggered by {other.name}", this);

        // Efecto simple: empuje vertical si tiene Rigidbody
        var rb = other.attachedRigidbody;
        if (rb != null && knockUpForce > 0f)
        {
            rb.AddForce(Vector3.up * knockUpForce, ForceMode.VelocityChange);
        }

        // UnityEvent
        onTriggered?.Invoke();

        // Mensaje opcional para que tu Player implemente el efecto real
        // (por ejemplo método: void OnTrapTriggered(TrapTile tile) { ... })
        if (sendMessageToPlayer)
        {
            other.gameObject.SendMessage("OnTrapTriggered", this, SendMessageOptions.DontRequireReceiver);
        }

        if (consumeOnTrigger)
        {
            // Si quieres un "cambio visual" en lugar de desaparecer, puedes
            // deshabilitar el collider y cambiar material aquí en vez de Destroy.
            gameObject.SetActive(false);
        }
    }
}

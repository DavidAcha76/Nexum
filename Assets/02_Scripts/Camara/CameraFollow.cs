
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target;

    [Header("Camera Config")]
    public Vector3 offset = new Vector3(0, 10, -5); // posición relativa al player
    public float smoothSpeed = 5f;

    void Update()
    {
        // 🔍 Si aún no tenemos referencia, intentamos buscar el Player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("[CameraFollow] Player encontrado y asignado");
            }
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // Posición deseada de la cámara
        Vector3 desiredPosition = target.position + offset;

        // Movimiento suave para seguir al player
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Mantener la cámara mirando al player (opcional)
        // Para top-down estilo Soul Knight, puedes comentar esta línea
        transform.LookAt(target);
    }
}

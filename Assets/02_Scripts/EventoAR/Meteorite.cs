using UnityEngine;

public class Meteorite : MonoBehaviour
{
    [SerializeField] private int hitsToDestroy = 5;  // pon 5 o 6 seg�n prefieras
    [SerializeField] private float smallShake = 0.05f;
    [SerializeField] private float shakeSpeed = 40f;

    private GameObject explosionPrefab;
    private System.Action<Vector3> onDestroyed;

    private int currentHits;

    // Llamado por el spawner despu�s de instanciar
    public void Setup(GameObject explosion, System.Action<Vector3> onDestroyedCallback)
    {
        explosionPrefab = explosion;
        onDestroyed = onDestroyedCallback;
    }

    public void ApplyHit()
    {
        currentHits++;
        // efecto de �golpe�: peque�a vibraci�n local
        float s = smallShake;
        transform.localPosition += Random.insideUnitSphere * s;

        if (currentHits >= hitsToDestroy)
        {
            Vector3 pos = transform.position;
            Destroy(gameObject);
            onDestroyed?.Invoke(pos);
        }
    }

    private void Update()
    {
        // animaci�n simple de rotaci�n para que se vea vivo
        transform.Rotate(0f, shakeSpeed * Time.deltaTime, 0f, Space.World);
    }
}

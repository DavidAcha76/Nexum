using System.Collections;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VideoIntroController intro;        // arrastra el VideoIntroController
    [SerializeField] private Camera arCamera;                   // AR Camera (Main Camera de XR Origin)
    [SerializeField] private GameObject meteoritePrefab;        // prefab del meteorito
    [SerializeField] private GameObject explosionVFXPrefab;     // prefab de partículas
    [SerializeField] private GameObject gameOverPanel;          // Panel "Moristes"
    [SerializeField] private MonoBehaviour tapToDamageComp;     // arrastra el componente TapToDamage de la AR Camera

    [Header("Spawn (arriba)")]
    [SerializeField] private float spawnHeight = 2.5f;
    [SerializeField] private float spawnHeightJitter = 0.5f;
    [SerializeField] private float forwardOffset = 0.6f;
    [SerializeField] private float yawRandom = 40f;

    [Header("Crecimiento")]
    [SerializeField] private float initialScaleFactor = 0.2f;
    [SerializeField] private float growthDuration = 10f;

    [Header("Derrota")]
    [SerializeField] private float timeToDie = 10f;             // segundos para mostrar "Moristes"
    [SerializeField] private float gameOverStaySeconds = 2f;    // cuánto mostrar el panel

    private GameObject spawnedMeteor;
    private Coroutine deathTimerCo;

    private IEnumerator Start()
    {
        // Espera a que termine el video de introducción
        while (intro != null && !intro.IntroFinished) yield return null;
        SpawnMeteor();
    }

    private void SpawnMeteor()
    {
        if (meteoritePrefab == null || arCamera == null) return;

        float height = spawnHeight + Random.Range(-spawnHeightJitter, spawnHeightJitter);
        float yaw = Random.Range(-yawRandom, yawRandom);
        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);
        Vector3 lateral = yawRot * arCamera.transform.right * Random.Range(-0.4f, 0.4f);

        Vector3 pos =
            arCamera.transform.position
            + arCamera.transform.up * height
            + arCamera.transform.forward * forwardOffset
            + lateral;

        spawnedMeteor = Instantiate(meteoritePrefab, pos, Quaternion.identity);
        spawnedMeteor.transform.LookAt(arCamera.transform);

        // tamaño inicial pequeño
        Vector3 finalScale = spawnedMeteor.transform.localScale;
        spawnedMeteor.transform.localScale = finalScale * Mathf.Clamp01(initialScaleFactor);
        StartCoroutine(GrowMeteor(spawnedMeteor.transform, finalScale, growthDuration));

        // asegurar script Meteorite y callback
        var m = spawnedMeteor.GetComponent<Meteorite>();
        if (m == null) m = spawnedMeteor.AddComponent<Meteorite>();
        m.Setup(explosionVFXPrefab, OnMeteorDestroyed);

        // habilitar input y arrancar temporizador de derrota
        if (tapToDamageComp != null) tapToDamageComp.enabled = true;
        if (deathTimerCo != null) StopCoroutine(deathTimerCo);
        deathTimerCo = StartCoroutine(DeathTimer());
    }

    private IEnumerator GrowMeteor(Transform t, Vector3 finalScale, float duration)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        float elapsed = 0f;

        while (elapsed < duration && t != null)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            t.localScale = Vector3.Lerp(start, finalScale, k);
            yield return null;
        }

        if (t != null) t.localScale = finalScale;
    }

    private IEnumerator DeathTimer()
    {
        float elapsed = 0f;
        while (elapsed < timeToDie)
        {
            if (spawnedMeteor == null) yield break; // ya se destruyó => no hay derrota
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Derrota: mostrar "Moristes" y bloquear input
        if (tapToDamageComp != null) tapToDamageComp.enabled = false;
        if (spawnedMeteor != null) Destroy(spawnedMeteor);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            yield return new WaitForSeconds(gameOverStaySeconds);
            // Aquí puedes elegir qué hacer tras mostrar "Moristes"
            // Ej: reiniciar escena actual o ir a otra. De momento, no hacemos nada más.
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Draft");
        }
    }

    private void OnMeteorDestroyed(Vector3 position)
    {
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, position, Quaternion.identity);

        if (deathTimerCo != null) StopCoroutine(deathTimerCo);

        // Cambio de escena tras victoria
        StartCoroutine(LoadNextSceneAfter(1.0f));
    }

    private IEnumerator LoadNextSceneAfter(float seconds)
    {
        if (tapToDamageComp != null) tapToDamageComp.enabled = false;
        yield return new WaitForSeconds(seconds);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Draft");
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if FUSION_WEAVED
using Fusion;
using UnityEngine.SceneManagement; // keep, used in fallback
#endif

/// <summary>
/// Adjunta este componente al enemigo "de salida".
/// Llama a NotifyKilled() cuando su lógica de muerte ocurra.
/// Cargará la siguiente escena (local o por Fusion si se configura).
/// </summary>
public class ExitOnDeath : MonoBehaviour
{
    [Header("Destino de escena")]
    [Tooltip("Si es >= 0 se usa BuildIndex; si es -1 y Name no está vacío, se usa Name.")]
    public int nextSceneBuildIndex = -1;
    public string nextSceneName = "";
    [Tooltip("Retardo antes de cambiar de escena (segundos)")]
    public float loadDelay = 0.5f;

    [Header("Network (opcional)")]
    [Tooltip("Usar cambio de escena con Fusion (Runner.LoadScene)")]
    public bool useFusionSceneLoad = false;

    bool _loading;

    /// <summary>Llama esto desde el script de vida del enemigo cuando muera.</summary>
    public void NotifyKilled()
    {
        if (_loading) return;
        _loading = true;
        StartCoroutine(CoLoadScene());
    }

    IEnumerator CoLoadScene()
    {
        if (loadDelay > 0f)
            yield return new WaitForSeconds(loadDelay);

        // Si se pidió usar Fusion y hay autoridad de escena, cargar sincronizado
#if FUSION_WEAVED
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (useFusionSceneLoad && runner != null)
        {
            if (runner.IsSceneAuthority)
            {
                if (nextSceneBuildIndex >= 0)
                {
                    var sref = SceneRef.FromIndex(nextSceneBuildIndex);
                    runner.LoadScene(sref, LoadSceneMode.Single);
                }
                else if (!string.IsNullOrWhiteSpace(nextSceneName))
                {
                    // SceneRef por nombre
                    var sref = SceneRef.FromName(nextSceneName);
                    runner.LoadScene(sref, LoadSceneMode.Single);
                }
                else
                {
                    Debug.LogError("[ExitOnDeath] No se configuró la escena destino.");
                }
            }
            yield break;
        }
#endif
        // Fallback local (singleplayer o sin autoridad)
        if (nextSceneBuildIndex >= 0)
            SceneManager.LoadScene(nextSceneBuildIndex, LoadSceneMode.Single);
        else if (!string.IsNullOrWhiteSpace(nextSceneName))
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        else
            Debug.LogError("[ExitOnDeath] No se configuró la escena destino.");
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject gameOverUI;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Asegúrate que el tiempo esté corriendo al inicio
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        Time.timeScale = 0f; // pausar juego
        if (gameOverUI) gameOverUI.SetActive(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

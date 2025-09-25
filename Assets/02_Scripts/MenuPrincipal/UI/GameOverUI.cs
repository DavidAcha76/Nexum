using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject gameOverPanel;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false); // oculto al inicio
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f; // congelar juego
    }

    public void Retry()
    {
        Time.timeScale = 1f; // reanudar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // recarga la escena actual
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        // 👉 Si tienes menú principal, pon el nombre de esa escena aquí
        SceneManager.LoadScene("MainMenu");

        // Si quieres salir del juego en build:
        // Application.Quit();
    }
}

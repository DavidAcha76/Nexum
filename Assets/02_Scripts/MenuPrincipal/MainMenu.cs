
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Cargar la escena del juego
    public void PlayGame()
    {
        SceneManager.LoadScene("Maze"); 
    }

    // Salir del juego
    public void QuitGame()
    {
        Debug.Log("Salir del juego...");
        Application.Quit();
    }
}

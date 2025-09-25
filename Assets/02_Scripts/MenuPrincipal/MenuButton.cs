using UnityEngine;
using UnityEngine.SceneManagement;


public class MenuButton : MonoBehaviour
{
    // 🔹 Llama esto desde el OnClick del botón
    public void GoToGame()
    {
        SceneManager.LoadScene("Draft");
    }

    // 🔹 Si quieres también salir del juego
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // para que funcione en el editor
#endif
    }
}

using System.Diagnostics;

using UnityEngine;

using UnityEngine.SceneManagement; // 👈 Necesario para cargar escenas

public class BossGoal : MonoBehaviour

{

    private void OnTriggerEnter(Collider other)

    {

        // ✅ Verificamos si el que tocó tiene el tag Player

        if (other.CompareTag("Player"))

        {


            SceneManager.LoadScene("EventAR"); // 👈 asegúrate que la escena está en Build Settings

        }

    }

}


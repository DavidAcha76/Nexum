
using UnityEngine;

public class CameraModeSwitcher : MonoBehaviour
{
    [Header("Refs")]
    public GameObject mainCamera;
    public GameObject arSession;
    public GameObject arSessionOrigin;

    private bool arActive = false;

    // Debe ser PUBLIC y VOID, sin parámetros
    public void SwitchMode()
    {
        arActive = !arActive;

        if (mainCamera) mainCamera.SetActive(!arActive);
        if (arSession) arSession.SetActive(arActive);
        if (arSessionOrigin) arSessionOrigin.SetActive(arActive);

        Debug.Log("Modo cambiado a: " + (arActive ? "AR" : "Normal"));
    }
}

using UnityEngine;

public class StatsUIController : MonoBehaviour
{
    public GameObject statsPanel; // referencia al panel de stats

    private bool isVisible = false;

    public void ToggleStats()
    {
        isVisible = !isVisible;
        statsPanel.SetActive(isVisible);
    }
}

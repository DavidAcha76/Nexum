using System.Collections;
using UnityEngine;

public class AlertUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject alertPanel;   // Asigna el Panel del Canvas
    [SerializeField] private float showSeconds = 2f;

    public bool AlertFinished { get; private set; }

    private void Start()
    {
        if (alertPanel != null) StartCoroutine(ShowThenHide());
        else AlertFinished = true; // por si te olvidas de asignar
    }

    private IEnumerator ShowThenHide()
    {
        alertPanel.SetActive(true);
        yield return new WaitForSeconds(showSeconds);
        alertPanel.SetActive(false);
        AlertFinished = true;
    }
}

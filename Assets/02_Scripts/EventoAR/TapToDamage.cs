using UnityEngine;

public class TapToDamage : MonoBehaviour
{
    [SerializeField] private Camera arCamera; // arrastra la AR Camera (o deja vacío y tomará Camera.main)
    [SerializeField] private float maxRayDistance = 20f;

    private void Awake()
    {
        if (arCamera == null) arCamera = GetComponent<Camera>();
        if (arCamera == null) arCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount == 0) return;

        Touch t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began) return;

        Ray ray = arCamera.ScreenPointToRay(t.position);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            // Buscamos el componente Meteorite en el objeto golpeado o sus padres
            var meteor = hit.collider.GetComponentInParent<Meteorite>();
            if (meteor != null) meteor.ApplyHit();
        }
    }
}

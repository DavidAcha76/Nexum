using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// Coloca el prefab (ej: RogueLikeMiniMazes) sobre un plano detectado al tocar
public class PlaceMazeOnPlane : MonoBehaviour
{
    [Header("Prefab del mundo (RogueLikeMiniMazes)")]
    public GameObject mazePrefab;

    [Header("Opciones")]
    public bool placeOnlyOnce = true;
    public bool hidePlaneMeshAfterPlace = true;

    [Header("Escala global del mapa")]
    [Range(10f, 300f)]
    public float mapScalePercent = 100f; // 100 = tamaño original

    // AR managers
    private ARRaycastManager _ray;
    private ARPlaneManager _planes;
    private ARAnchorManager _anchors;

    private static readonly List<ARRaycastHit> _hits = new();

    // Estado
    private Transform _mazeAnchor;
    private bool _placed;

    void Awake()
    {
        _ray = GetComponent<ARRaycastManager>();
        _planes = GetComponent<ARPlaneManager>();
        _anchors = GetComponent<ARAnchorManager>();
    }

    void Update()
    {
        if (placeOnlyOnce && _placed) return;
        if (Input.touchCount == 0) return;

        var t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began) return;

        if (_ray.Raycast(t.position, _hits, TrackableType.PlaneWithinPolygon))
        {
            var hit = _hits[0];
            var pose = hit.pose;

            // 1) Crear anchor
            Transform parent = CreateAnchor(hit, pose, out ARPlane plane);

            // 2) Aplicar escala global del mapa (antes de construir)
            ApplyScaleToAnchor(parent);

            // 3) Instanciar el RogueLikeMiniMazes bajo ese anchor
            if (mazePrefab != null)
            {
                var go = Instantiate(mazePrefab, pose.position, pose.rotation, parent);

                // Llamar SetAnchor para que construya centrado y como hijos del anchor
                var gen = go.GetComponent<RogueLikeMiniMazes>();
                if (gen != null)
                {
                    gen.autoBuildOnStart = false;     // nos encargamos nosotros
                    gen.SetAnchor(parent, rebuild: true);
                }
            }

            // 4) Opcional: ocultar el plano usado
            if (hidePlaneMeshAfterPlace && plane)
            {
                var vis = plane.GetComponent<ARPlaneMeshVisualizer>();
                var rend = plane.GetComponent<MeshRenderer>();
                if (vis) vis.enabled = false;
                if (rend) rend.enabled = false;
            }

            _placed = true;
        }
    }

    Transform CreateAnchor(ARRaycastHit hit, Pose pose, out ARPlane plane)
    {
        plane = null;
        Transform parent = null;

        if (_planes) plane = _planes.GetPlane(hit.trackableId);
        if (_anchors && plane)
        {
            var anchor = _anchors.AttachAnchor(plane, pose);
            if (anchor) parent = anchor.transform;
        }

        if (parent == null)
        {
            var go = new GameObject("MazeAnchor");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            parent = go.transform;
        }

        if (_mazeAnchor && _mazeAnchor != parent)
            Destroy(_mazeAnchor.gameObject);

        _mazeAnchor = parent;
        _mazeAnchor.name = "MazeAnchor";
        return parent;
    }

    void ApplyScaleToAnchor(Transform parent)
    {
        float k = Mathf.Max(0.01f, mapScalePercent / 100f);
        parent.localScale = Vector3.one * k;
    }
}

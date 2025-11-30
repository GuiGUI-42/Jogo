using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class CenterCameraOnTilemap : MonoBehaviour
{
    public Tilemap tilemap;
    public Camera cam;
    public float padding = 0.5f;
    public bool onlyOnStart = true;

    [Header("Zoom")]
    [Tooltip("1 = cabe exatamente; <1 aproxima; >1 afasta")]
    [Min(0.01f)] public float zoomFactor = 1f;
    public bool enableScrollZoom = false;
    [Tooltip("Intensidade por 'passo' de scroll (≈1 passo = 120 no Input System)")]
    public float zoomSpeed = 0.1f;
    public float minZoomFactor = 0.3f;
    public float maxZoomFactor = 2f;
    [Tooltip("Inverter direção do scroll (true = rolar para cima aproxima)")]
    public bool invertScrollDirection = false;

    [Header("Manual")]
    public bool usarSizeManual = false; // Marque para usar o size do Inspector

    [Header("Pan (Arrastar)")]
    public bool enableDragPan = true;
    [Tooltip("0=Esquerdo, 1=Direito, 2=Meio (no Input legado)")]
    public int panMouseButton = 0;

    // Estado interno
    bool isPanning;
    Vector3 panGrabOffset; // camPos - mouseWorld no início do pan
    Bounds worldBounds;

    void OnValidate()
    {
        if (!cam) cam = GetComponent<Camera>();
        RecalcWorldBounds();
        CenterAndFit();
    }

    void Start()
    {
        if (!cam) cam = Camera.main;
        RecalcWorldBounds();
        CenterAndFit();
    }

    void Update()
    {
        if (enableScrollZoom && !usarSizeManual)
        {
            float scroll = GetScrollDeltaNormalized();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float dir = invertScrollDirection ? 1f : -1f; // por padrão, para cima aproxima
                zoomFactor = Mathf.Clamp(zoomFactor + dir * scroll * zoomSpeed, minZoomFactor, maxZoomFactor);
                FitSizeOnly(); // não recentra ao dar zoom com scroll
            }
        }

        if (enableDragPan)
        {
            HandlePan();
        }
    }

    void LateUpdate()
    {
        if (!onlyOnStart && !usarSizeManual)
        {
            // Mantém o size ajustado caso a tela mude, sem recentrar a posição
            FitSizeOnly();
        }
    }

    void CenterAndFit()
    {
        if (!tilemap || !cam) return;
        RecalcWorldBounds();
        RecenterToBoundsCenter();
        FitSizeOnly();
        // Se usarSizeManual estiver marcado, FitSizeOnly preserva size atual
    }

    void FitSizeOnly()
    {
        if (!tilemap || !cam) return;
        if (cam.orthographic && !usarSizeManual)
        {
            var b = tilemap.localBounds;
            float width = b.size.x + padding * 2f;
            float height = b.size.y + padding * 2f;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = width / height;

            float baseSize = (screenRatio >= targetRatio)
                ? height / 2f
                : (width / 2f) / screenRatio;

            cam.orthographicSize = baseSize * zoomFactor;
        }

        // Recalcula bounds e aplica clamp após mudanças de size
        RecalcWorldBounds();
        cam.transform.position = ClampToBounds(cam.transform.position);
    }

    void RecenterToBoundsCenter()
    {
        Vector3 worldCenter = worldBounds.center;
        cam.transform.position = new Vector3(worldCenter.x, worldCenter.y, cam.transform.position.z);
        cam.transform.position = ClampToBounds(cam.transform.position);
    }

    void RecalcWorldBounds()
    {
        if (!tilemap) return;
        tilemap.CompressBounds();
        var lb = tilemap.localBounds;
        var center = lb.center;
        var half = lb.extents;
        // Converte min/max locais para mundo
        Vector3 localMin = center - half;
        Vector3 localMax = center + half;
        Vector3 worldMin = tilemap.transform.TransformPoint(localMin);
        Vector3 worldMax = tilemap.transform.TransformPoint(localMax);
        // Ajusta padding em mundo
        worldMin.x -= padding; worldMin.y -= padding;
        worldMax.x += padding; worldMax.y += padding;
        // Reconstrói bounds
        worldBounds = new Bounds();
        worldBounds.SetMinMax(
            new Vector3(Mathf.Min(worldMin.x, worldMax.x), Mathf.Min(worldMin.y, worldMax.y), 0f),
            new Vector3(Mathf.Max(worldMin.x, worldMax.x), Mathf.Max(worldMin.y, worldMax.y), 0f)
        );
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        if (!cam || !cam.orthographic) return pos;
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * ((float)Screen.width / Screen.height);

        float minX = worldBounds.min.x + horzExtent;
        float maxX = worldBounds.max.x - horzExtent;
        float minY = worldBounds.min.y + vertExtent;
        float maxY = worldBounds.max.y - vertExtent;

        // Se o mapa for menor que a tela, evita NaN e fixa no centro
        if (minX > maxX) { float cx = worldBounds.center.x; minX = maxX = cx; }
        if (minY > maxY) { float cy = worldBounds.center.y; minY = maxY = cy; }

        float x = Mathf.Clamp(pos.x, minX, maxX);
        float y = Mathf.Clamp(pos.y, minY, maxY);
        return new Vector3(x, y, pos.z);
    }

    void HandlePan()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        bool down = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (panMouseButton == 1) down = Mouse.current != null && Mouse.current.rightButton.isPressed;
        else if (panMouseButton == 2) down = Mouse.current != null && Mouse.current.middleButton.isPressed;

        if (!isPanning && down)
        {
            isPanning = true;
            var mw = GetMouseWorld();
            panGrabOffset = cam.transform.position - new Vector3(mw.x, mw.y, cam.transform.position.z);
        }
        else if (isPanning && !down)
        {
            isPanning = false;
        }

        if (isPanning)
        {
            Vector3 mw = GetMouseWorld();
            Vector3 target = new Vector3(mw.x, mw.y, cam.transform.position.z) + panGrabOffset;
            cam.transform.position = ClampToBounds(target);
        }
#else
        bool down = Input.GetMouseButton(panMouseButton);
        if (!isPanning && down)
        {
            isPanning = true;
            var mw = GetMouseWorld();
            panGrabOffset = cam.transform.position - new Vector3(mw.x, mw.y, cam.transform.position.z);
        }
        else if (isPanning && !down)
        {
            isPanning = false;
        }

        if (isPanning)
        {
            Vector3 mw = GetMouseWorld();
            Vector3 target = new Vector3(mw.x, mw.y, cam.transform.position.z) + panGrabOffset;
            cam.transform.position = ClampToBounds(target);
        }
#endif
    }

    // Normaliza o scroll para "passos" (~1 por notch). Suporta Input System e Input legado.
    float GetScrollDeltaNormalized()
    {
        float v = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null)
        {
            // Geralmente 120 por passo; trackpads dão valores menores. Normalizamos.
            v = Mouse.current.scroll.ReadValue().y / 120f;
        }
#else
        // Input Manager legado: valor já é pequeno (≈ ±0.1 por passo)
        v = Input.GetAxis("Mouse ScrollWheel");
#endif
        return v;
    }

    Vector3 GetMouseWorld()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Vector2 sp = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 sp = Input.mousePosition;
#endif
        float planeZ = tilemap != null ? tilemap.transform.position.z : 0f;
        float dist = Mathf.Abs(planeZ - cam.transform.position.z);
        var wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, dist));
        wp.z = cam.transform.position.z; // manter z da câmera, só usamos x/y
        return wp;
    }
}
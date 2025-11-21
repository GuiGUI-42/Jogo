using UnityEngine;

/// <summary>
/// Faz um elemento de UI (RectTransform) seguir um objeto no mundo (Transform).
/// </summary>
public class UIFollowWorldObject : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetTarget(Transform worldTarget, Vector3 spawnOffset)
    {
        this.target = worldTarget;
        this.offset = spawnOffset;
        UpdatePosition();
    }

    void LateUpdate()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        if (target == null || mainCamera == null || canvas == null) return;

        // Posição do alvo no mundo + offset definido no EventoLocalPoint
        Vector3 worldPos = target.position + offset;

        // Converte para posição de tela
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPos);

        // Verifica se está atrás da câmera (opcional, útil para 3D)
        if (screenPoint.z < 0) 
        {
            rectTransform.gameObject.SetActive(false);
            return;
        }
        else if (!rectTransform.gameObject.activeSelf)
        {
            rectTransform.gameObject.SetActive(true);
        }

        // Se o Canvas for Screen Space - Overlay
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = screenPoint;
        }
        // Se o Canvas for Screen Space - Camera (comum em jogos 2D com pixel art)
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, 
                screenPoint, 
                mainCamera, 
                out localPos
            );
            rectTransform.anchoredPosition = localPos;
        }
    }
}
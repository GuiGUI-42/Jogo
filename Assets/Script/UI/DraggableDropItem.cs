using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Payload do Drop")]
    public Item item;
    public int quantidade = 1;
    [Tooltip("Asset genérico (ItemCombate, etc.) usado se 'item' estiver nulo.")]
    public ScriptableObject asset;

    [Header("Referências")]
    public Image sourceImage; // a Image que mostra o ícone do item

    Canvas canvas;
    GameObject dragIcon;
    CanvasGroup sourceCanvasGroup;

    void Awake()
    {
        if (!sourceImage) sourceImage = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            var c = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (!c) c = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (c) canvas = c;
        }
        sourceCanvasGroup = GetComponent<CanvasGroup>();
        if (!sourceCanvasGroup) sourceCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (sourceImage == null || canvas == null) return;
        // Permite drag mesmo sem 'item' se houver 'asset' genérico com sprite
        if (item == null && asset == null) return;
        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvas.transform, false);
        var img = dragIcon.GetComponent<Image>();
        img.sprite = sourceImage.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        (dragIcon.transform as RectTransform).sizeDelta = (sourceImage.transform as RectTransform).rect.size;

        sourceCanvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        sourceCanvasGroup.alpha = 1f;
    }
}

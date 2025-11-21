using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System; // Necessário para Action

public class DraggableDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Payload do Drop")]
    public Item item;
    public int quantidade = 1;
    [Tooltip("Asset genérico (ItemCombate, etc.) usado se 'item' estiver nulo.")]
    public ScriptableObject asset;

    [Header("Referências")]
    public Image sourceImage;

    // Evento para avisar quem estiver ouvindo (EventoCombateUI) que o item foi entregue
    public event Action OnItemArrastadoComSucesso;

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
        if (item == null && asset == null) return;

        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvas.transform, false);
        var img = dragIcon.GetComponent<Image>();
        img.sprite = sourceImage.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        (dragIcon.transform as RectTransform).sizeDelta = (sourceImage.transform as RectTransform).rect.size;

        sourceCanvasGroup.alpha = 0.6f;
        sourceCanvasGroup.blocksRaycasts = false; // Importante: Permitir que o raycast passe para o alvo (Bag)
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
        sourceCanvasGroup.blocksRaycasts = true;
    }

    // Método chamado pelos "Recebedores" (BagDropTarget, InventorySlotDropTarget)
    public void NotificarSucesso()
    {
        Debug.Log("[DraggableDropItem] Sucesso notificado! Disparando evento...");
        OnItemArrastadoComSucesso?.Invoke();
    }
}
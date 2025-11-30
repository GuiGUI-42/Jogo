using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableBagSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScriptableObject asset;
    public Item item; // se também for Item
    public int quantidade = 1;
    public int inventoryIndex = -1;

    public Image sourceImage;
    Canvas canvas;
    GameObject dragIcon;
    CanvasGroup cg;

    void Awake()
    {
        if (!sourceImage) sourceImage = GetComponent<Image>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            var c = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (!c) c = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            canvas = c;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!sourceImage || !canvas) return;
        // Sempre consulta a fonte de verdade: InventoryManager
        if (inventoryIndex < 0 || InventoryManager.Instance == null) return;
        var entries = InventoryManager.Instance.itens;
        if (inventoryIndex >= entries.Count) return;
        var atual = entries[inventoryIndex].asset;
        if (atual == null) return;
        asset = atual;
        item = atual as Item;
        quantidade = entries[inventoryIndex].quantidade;
        Debug.Log("[DragBagSlot] BeginDrag index=" + inventoryIndex + " asset=" + asset?.name + " qtd=" + quantidade);
        dragIcon = new GameObject("DragIcon_Bag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvas.transform, false);
        var img = dragIcon.GetComponent<Image>();
        img.sprite = sourceImage.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        (dragIcon.transform as RectTransform).sizeDelta = (sourceImage.transform as RectTransform).rect.size;
        cg.alpha = 0.6f;
        cg.blocksRaycasts = false; // permite que alvo receba o drop
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon) dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon) Destroy(dragIcon);
        cg.alpha = 1f;
        cg.blocksRaycasts = true; // restaura raycasts
        Debug.Log("[DragBagSlot] EndDrag index=" + inventoryIndex);
    }
}

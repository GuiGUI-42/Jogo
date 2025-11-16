using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableHeroInventarioSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public HeroiAtributos heroiAtributos; // origem
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
            var c = Object.FindFirstObjectByType<Canvas>();
            if (!c) c = Object.FindAnyObjectByType<Canvas>();
            canvas = c;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!sourceImage || !canvas) return;
        // Sempre consulta a fonte de verdade: slots do herói
        if (heroiAtributos == null || inventoryIndex < 0 || inventoryIndex >= heroiAtributos.slotsInventario.Length)
            return;
        var atual = heroiAtributos.slotsInventario[inventoryIndex];
        if (atual == null) return;
        asset = atual;
        item = atual as Item;
        Debug.Log("[DragHeroSlot] BeginDrag hero=" + heroiAtributos?.name + " index=" + inventoryIndex + " asset=" + asset?.name);
        dragIcon = new GameObject("DragIcon_Hero", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvas.transform, false);
        var img = dragIcon.GetComponent<Image>();
        img.sprite = sourceImage.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        (dragIcon.transform as RectTransform).sizeDelta = (sourceImage.transform as RectTransform).rect.size;
        cg.alpha = 0.6f;
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon) dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon) Destroy(dragIcon);
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        Debug.Log("[DragHeroSlot] EndDrag hero=" + heroiAtributos?.name + " index=" + inventoryIndex);
    }
}

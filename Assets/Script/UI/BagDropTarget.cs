using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BagDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    Image img;
    Color originalColor;

    void Awake()
    {
        img = GetComponent<Image>();
        originalColor = img ? img.color : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (img) img.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.85f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (img) img.color = originalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (img) img.color = originalColor;
        var dragged = eventData.pointerDrag;
        if (!dragged) return;
        var d = dragged.GetComponent<DraggableDropItem>();
        if (d != null)
        {
            var quantidade = Mathf.Max(1, d.quantidade);
            if (d.item != null)
            {
                InventoryManager.Instance.Add(d.item, quantidade);
            }
            else if (d.asset != null)
            {
                InventoryManager.Instance.AddAsset(d.asset, quantidade);
            }

            if (d.item != null || d.asset != null)
            {
                // Fecha a UI de combate após coletar o item
                if (EventoCombateUI.Instance != null)
                {
                    EventoCombateUI.Instance.Fechar();
                }
            }
        }
    }
}

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
        Debug.Log("[BagDropTarget] Awake raycastTarget=" + (img ? img.raycastTarget.ToString() : "nullImage"));
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

        // ---------------------------------------------------------
        // CASO 1: Drop vindo de combate (Item Dropado)
        // ---------------------------------------------------------
        var dropItem = dragged.GetComponent<DraggableDropItem>();
        if (dropItem != null)
        {
            var quantidade = Mathf.Max(1, dropItem.quantidade);
            bool adicionou = false;

            if (dropItem.item != null) 
            {
                InventoryManager.Instance.Add(dropItem.item, quantidade);
                adicionou = true;
            }
            else if (dropItem.asset != null) 
            {
                InventoryManager.Instance.AddAsset(dropItem.asset, quantidade);
                adicionou = true;
            }

            if (adicionou)
            {
                Debug.Log("[BagDropTarget] Recebeu drop de combate com sucesso.");
                // AVISA O ITEM QUE ELE FOI ACEITO (Para fechar a UI de Combate)
                dropItem.NotificarSucesso(); 
            }
            return;
        }

        // ---------------------------------------------------------
        // CASO 2: Drop vindo de inventário de herói (Remover do herói -> Bag)
        // ---------------------------------------------------------
        var heroSlot = dragged.GetComponent<DraggableHeroInventarioSlot>();
        if (heroSlot != null && (heroSlot.asset || heroSlot.item))
        {
            var asset = heroSlot.asset ? heroSlot.asset : heroSlot.item;
            InventoryManager.Instance.AddAsset(asset, Mathf.Max(1, heroSlot.quantidade));
            if (heroSlot.heroiAtributos != null)
            {
                if (heroSlot.inventoryIndex >= 0)
                    heroSlot.heroiAtributos.RemoverAssetNoIndice(heroSlot.inventoryIndex);
                else
                    heroSlot.heroiAtributos.RemoverAsset(asset);
            }
            AtualizarUIs();
            Debug.Log("[BagDrop] Item movido do herói para a Bag: " + asset.name);
            return;
        }
        
        Debug.Log("[BagDropTarget] Drop ignorado: sem componente reconhecido.");
    }

    void AtualizarUIs()
    {
        var heroisMenores = FindObjectsByType<HeroiInventarioUI>(FindObjectsSortMode.None);
        foreach (var ui in heroisMenores) ui.AtualizarInventario();
        var heroisMaiores = FindObjectsByType<InventarioHeroiUI>(FindObjectsSortMode.None);
        foreach (var ui in heroisMaiores) ui.AtualizarInventario();
        var bags = FindObjectsByType<BagInventoryUI>(FindObjectsSortMode.None);
        foreach (var b in bags) b.RefreshSlots();
    }
}
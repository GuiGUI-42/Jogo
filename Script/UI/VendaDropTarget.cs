using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class VendaDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    Image img;
    Color originalColor;

    [Header("Feedback Visual")]
    public Color hoverColor = Color.green; // Fica verde quando passa o mouse com item

    void Awake()
    {
        img = GetComponent<Image>();
        originalColor = img ? img.color : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged && img)
        {
            // Só muda de cor se for um item vendável (da bag ou heroi)
            if (dragged.GetComponent<DraggableBagSlot>() || dragged.GetComponent<DraggableHeroInventarioSlot>())
            {
                img.color = hoverColor;
            }
        }
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

        ScriptableObject assetVendido = null;
        int quantidade = 1;

        // 1. Tenta pegar da BAG
        var bagSlot = dragged.GetComponent<DraggableBagSlot>();
        if (bagSlot != null && InventoryManager.Instance != null)
        {
            // Pega o asset antes de remover
            if(bagSlot.inventoryIndex >= 0 && bagSlot.inventoryIndex < InventoryManager.Instance.itens.Count)
            {
                var entry = InventoryManager.Instance.itens[bagSlot.inventoryIndex];
                assetVendido = entry.asset;
                
                // --- CORREÇÃO AQUI ---
                // Antes: quantidade = entry.amount;
                // Agora:
                quantidade = entry.quantidade; 
                
                // Remove da Bag (Vende o stack todo)
                InventoryManager.Instance.RemoveAsset(assetVendido, quantidade);
            }
        }

        // 2. Tenta pegar do HERÓI
        var heroSlot = dragged.GetComponent<DraggableHeroInventarioSlot>();
        if (heroSlot != null && heroSlot.heroiAtributos != null)
        {
            // Pega o asset
            if (heroSlot.inventoryIndex >= 0)
                assetVendido = heroSlot.heroiAtributos.slotsInventario[heroSlot.inventoryIndex];
            else if (heroSlot.asset)
                assetVendido = heroSlot.asset;
            else 
                assetVendido = heroSlot.item;

            // Remove do Herói
            if (assetVendido != null)
            {
                if (heroSlot.inventoryIndex >= 0)
                    heroSlot.heroiAtributos.RemoverAssetNoIndice(heroSlot.inventoryIndex);
                else
                    heroSlot.heroiAtributos.RemoverAsset(assetVendido);
                
                AtualizarUIsHeroi(); // Atualiza visual
            }
        }

        // 3. Verifica se veio da Loja (DraggableDropItem) - BLOQUEAR
        var dropItem = dragged.GetComponent<DraggableDropItem>();
        if (dropItem != null)
        {
            Debug.Log("Não pode vender um item diretamente da loja ou drop!");
            return;
        }

        // --- EFETUAR VENDA ---
        if (assetVendido != null)
        {
            int valorVenda = 0;

            if (assetVendido is Item itemData)
            {
                valorVenda = itemData.GetValorVenda();
            }
            else
            {
                // Fallback para itens antigos sem scriptable object 'Item'
                valorVenda = 10; 
            }

            int totalGanho = valorVenda * quantidade; 

            GameManager.instance.GanharOuro(totalGanho);
            Debug.Log($"Vendeu {assetVendido.name} (x{quantidade}) por {totalGanho} ouro.");
        }
    }

    void AtualizarUIsHeroi()
    {
        // Força atualização das UIs dos heróis para o item sumir visualmente
        var uis = FindObjectsByType<HeroiInventarioUI>(FindObjectsSortMode.None);
        foreach (var ui in uis) ui.AtualizarInventario();
        var uisMain = FindObjectsByType<InventarioHeroiUI>(FindObjectsSortMode.None);
        foreach (var ui in uisMain) ui.AtualizarInventario();
    }
}
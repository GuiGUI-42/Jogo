using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop target genérico para um slot de inventário (herói ou bag).
/// </summary>
public class InventorySlotDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotTipo { Hero, Bag }
    public SlotTipo slotTipo;
    public HeroiAtributos heroAtributos; // somente se SlotTipo.Hero
    public int slotIndex = -1; // hero: índice real; bag: índice visual
    public bool hoverHighlight = false;

    Image img;
    Color baseColor;

    void Awake()
    {
        img = GetComponent<Image>();
        if (img) baseColor = img.color; else baseColor = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData)
    {
        if (img) img.color = baseColor;
        var dragged = eventData.pointerDrag;
        if (!dragged) return;

        // Determina payload
        var dragHero = dragged.GetComponent<DraggableHeroInventarioSlot>();
        var dragBag = dragged.GetComponent<DraggableBagSlot>();
        var dragDrop = dragged.GetComponent<DraggableDropItem>();

        // =================================================================================
        // TIPO: SLOT DE HERÓI
        // =================================================================================
        if (slotTipo == SlotTipo.Hero)
        {
            if (!heroAtributos)
            {
                Debug.LogWarning("[InvSlot] Slot herói sem heroAtributos.");
                return;
            }
            if (slotIndex < 0 || slotIndex >= heroAtributos.slotsInventario.Length)
            {
                Debug.LogWarning("[InvSlot] Índice inválido para slot herói:" + slotIndex);
                return;
            }

            // Hero -> Hero (swap/move)
            if (dragHero && dragHero.heroiAtributos == heroAtributos)
            {
                int origem = dragHero.inventoryIndex;
                if (origem == slotIndex) return; // soltou no mesmo lugar
                var slots = heroAtributos.slotsInventario;
                var temp = slots[origem];
                slots[origem] = slots[slotIndex];
                slots[slotIndex] = temp;
                Debug.Log($"[InvSlot] Swap hero slots origem={origem} destino={slotIndex}");
                heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                return;
            }

            // Outro herói -> este herói
            if (dragHero && dragHero.heroiAtributos != heroAtributos)
            {
                ScriptableObject asset = null;
                if (dragHero.heroiAtributos != null && dragHero.inventoryIndex >= 0)
                    asset = dragHero.heroiAtributos.slotsInventario[dragHero.inventoryIndex];
                if (!asset) return;

                var slots = heroAtributos.slotsInventario;
                var slotsOrigem = dragHero.heroiAtributos.slotsInventario;

                if (slots[slotIndex] == null)
                {
                    slots[slotIndex] = asset;
                    if (dragHero.inventoryIndex >= 0) slotsOrigem[dragHero.inventoryIndex] = null;
                }
                else
                {
                    var temp = slots[slotIndex];
                    slots[slotIndex] = asset;
                    if (dragHero.inventoryIndex >= 0) slotsOrigem[dragHero.inventoryIndex] = temp;
                }
                heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                dragHero.heroiAtributos.OnInventarioAlterado?.Invoke(dragHero.heroiAtributos);
                return;
            }

            // Bag -> herói
            if (dragBag)
            {
                ScriptableObject asset = null;
                if (InventoryManager.Instance != null && dragBag.inventoryIndex >= 0 && dragBag.inventoryIndex < InventoryManager.Instance.itens.Count)
                    asset = InventoryManager.Instance.itens[dragBag.inventoryIndex].asset;
                if (!asset) return;

                var slots = heroAtributos.slotsInventario;
                if (slots[slotIndex] == null)
                {
                    heroAtributos.RemoverAsset(asset); // Evita duplicatas
                    if (InventoryManager.Instance.RemoveAsset(asset, 1))
                    {
                        slots[slotIndex] = asset;
                        heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                    }
                }
                else
                {
                    var antigo = slots[slotIndex];
                    if (InventoryManager.Instance.RemoveAsset(asset, 1))
                    {
                        slots[slotIndex] = asset;
                        InventoryManager.Instance.AddAsset(antigo, 1);
                        heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                    }
                }
                return;
            }

            // ---------------------------------------------------------
            // Drop de combate -> herói no slot exato (NOTIFICAR SUCESSO AQUI)
            // ---------------------------------------------------------
            if (dragDrop)
            {
                var asset = dragDrop.asset ? dragDrop.asset : (ScriptableObject)dragDrop.item;
                if (!asset) return;
                var slots = heroAtributos.slotsInventario;
                
                // Substitui ou coloca no slot
                slots[slotIndex] = asset;
                
                Debug.Log($"[InvSlot] Drop combate -> herói asset={asset.name} slot={slotIndex}");
                heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                
                // IMPORTANTE: Avisa o drop
                dragDrop.NotificarSucesso();
                return;
            }
        }
        // =================================================================================
        // TIPO: SLOT DE BAG
        // =================================================================================
        else 
        {
            // Herói -> Bag
            if (dragHero)
            {
                ScriptableObject asset = null;
                if (dragHero.heroiAtributos != null && dragHero.inventoryIndex >= 0)
                    asset = dragHero.heroiAtributos.slotsInventario[dragHero.inventoryIndex];
                if (!asset) return;
                if (dragHero.heroiAtributos.RemoverAssetNoIndice(dragHero.inventoryIndex))
                {
                    InventoryManager.Instance.AddAsset(asset, 1);
                    dragHero.heroiAtributos.OnInventarioAlterado?.Invoke(dragHero.heroiAtributos);
                }
                return;
            }

            // ---------------------------------------------------------
            // Combate -> Bag (Slot específico) (NOTIFICAR SUCESSO AQUI)
            // ---------------------------------------------------------
            if (dragDrop)
            {
                var asset = dragDrop.asset ? dragDrop.asset : (ScriptableObject)dragDrop.item;
                if (!asset) return;
                InventoryManager.Instance.AddAsset(asset, Mathf.Max(1, dragDrop.quantidade));
                Debug.Log($"[InvSlot] Drop combate -> bag asset={asset.name}");
                
                // IMPORTANTE: Avisa o drop
                dragDrop.NotificarSucesso();
                return;
            }

            // Bag -> Bag (reorder)
            if (dragBag)
            {
                if (InventoryManager.Instance == null) return;
                var list = InventoryManager.Instance.itens;
                int from = dragBag.inventoryIndex;
                int to = slotIndex;
                if (from == to) return;
                if (from >= 0 && from < list.Count && to >= 0 && to < list.Count)
                {
                    var temp = list[from];
                    list.RemoveAt(from);
                    list.Insert(to, temp);
                    InventoryManager.Instance.RaiseInventoryChanged();
                }
                return;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop target genérico para um slot de inventário (herói ou bag).
/// - Em herói: move/swap itens dentro do mesmo herói ou recebe da bag / combate.
/// - Em bag: move da bag para herói (se soltar em slot de herói) ou recebe de herói / combate para adicionar à bag.
/// Para bag, 'heroAtributos' deve ser nulo e 'slotTipo' = Bag; índice representa posição visual, mas InventoryManager controla ordem real.
/// Para herói, 'slotTipo' = Hero e heroAtributos preenchido; indice = posição fixa no array.
/// </summary>
public class InventorySlotDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotTipo { Hero, Bag }
    public SlotTipo slotTipo;
    public HeroiAtributos heroAtributos; // somente se SlotTipo.Hero
    public int slotIndex = -1; // hero: índice real; bag: índice visual
    public bool hoverHighlight = false; // desativado por padrão (sem efeito visual)

    Image img;
    Color baseColor;
    GameObject highlight; // não utilizado quando hoverHighlight = false

    void Awake()
    {
        img = GetComponent<Image>();
        if (img) baseColor = img.color; else baseColor = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mantido vazio para permitir futura assinatura sem efeito visual.
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        // Mantido vazio para permitir futura assinatura sem efeito visual.
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (img) img.color = baseColor;
        var dragged = eventData.pointerDrag;
        if (!dragged) return;

        // Determina payload
        var dragHero = dragged.GetComponent<DraggableHeroInventarioSlot>();
        var dragBag = dragged.GetComponent<DraggableBagSlot>();
        var dragDrop = dragged.GetComponent<DraggableDropItem>();

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
                Debug.Log($"[InvSlot] Swap hero slots origem={origem} destino={slotIndex} heroi={heroAtributos.name}");
                heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                return;
            }

            // Outro herói -> este herói
            if (dragHero && dragHero.heroiAtributos != heroAtributos)
            {
                // Sempre usa estado atual do slot origem
                ScriptableObject asset = null;
                if (dragHero.heroiAtributos != null && dragHero.inventoryIndex >= 0 && dragHero.inventoryIndex < dragHero.heroiAtributos.slotsInventario.Length)
                    asset = dragHero.heroiAtributos.slotsInventario[dragHero.inventoryIndex];
                if (!asset) return;
                // Se slot destino vazio, move; senão tenta swap
                var slots = heroAtributos.slotsInventario;
                var slotsOrigem = dragHero.heroiAtributos.slotsInventario;
                if (slots[slotIndex] == null)
                {
                    slots[slotIndex] = asset;
                    // limpa origem
                    if (dragHero.inventoryIndex >= 0 && dragHero.inventoryIndex < slotsOrigem.Length)
                        slotsOrigem[dragHero.inventoryIndex] = null;
                    Debug.Log($"[InvSlot] Move entre heróis asset={asset.name} para slot={slotIndex} destino={heroAtributos.name}");
                }
                else
                {
                    // swap entre heróis
                    var temp = slots[slotIndex];
                    slots[slotIndex] = asset;
                    if (dragHero.inventoryIndex >= 0 && dragHero.inventoryIndex < slotsOrigem.Length)
                        slotsOrigem[dragHero.inventoryIndex] = temp;
                    Debug.Log($"[InvSlot] Swap entre heróis asset={asset.name} slotDestino={slotIndex}");
                }
                heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                dragHero.heroiAtributos.OnInventarioAlterado?.Invoke(dragHero.heroiAtributos);
                return;
            }

            // Bag -> herói (coloca no slot específico se vazio; se cheio: swap)
            if (dragBag)
            {
                // Sempre usa estado atual da bag
                ScriptableObject asset = null;
                if (InventoryManager.Instance != null && dragBag.inventoryIndex >= 0 && dragBag.inventoryIndex < InventoryManager.Instance.itens.Count)
                    asset = InventoryManager.Instance.itens[dragBag.inventoryIndex].asset;
                if (!asset) return;
                var slots = heroAtributos.slotsInventario;
                if (slots[slotIndex] == null)
                {
                    // Remove instância prévia do mesmo asset em outro slot (opcional)
                    heroAtributos.RemoverAsset(asset);
                    if (InventoryManager.Instance.RemoveAsset(asset, 1))
                    {
                        slots[slotIndex] = asset;
                        Debug.Log($"[InvSlot] Move bag->herói asset={asset.name} slot={slotIndex}");
                        heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                    }
                }
                else
                {
                    // swap: slot destino vai para bag e asset entra
                    var antigo = slots[slotIndex];
                    if (InventoryManager.Instance.RemoveAsset(asset, 1))
                    {
                        slots[slotIndex] = asset;
                        InventoryManager.Instance.AddAsset(antigo, 1);
                        Debug.Log($"[InvSlot] Swap bag<->herói novo={asset.name} antigo={antigo?.name} slot={slotIndex}");
                        heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                    }
                }
                return;
            }

            // Drop de combate -> herói no slot exato
            if (dragDrop)
            {
                var asset = dragDrop.asset ? dragDrop.asset : (ScriptableObject)dragDrop.item;
                if (!asset) return;
                var slots = heroAtributos.slotsInventario;
                if (slots[slotIndex] == null)
                {
                    slots[slotIndex] = asset;
                    Debug.Log($"[InvSlot] Drop combate -> herói asset={asset.name} slot={slotIndex}");
                }
                else
                {
                    var antigo = slots[slotIndex];
                    slots[slotIndex] = asset;
                    Debug.Log($"[InvSlot] Replace combate -> herói asset={asset.name} slot={slotIndex} antigo={antigo?.name}");
                }
                heroAtributos.OnInventarioAlterado?.Invoke(heroAtributos);
                return;
            }
        }
        else // Bag slots
        {
            // Herói -> Bag
            if (dragHero)
            {
                // Sempre usa estado atual do slot origem
                ScriptableObject asset = null;
                if (dragHero.heroiAtributos != null && dragHero.inventoryIndex >= 0 && dragHero.inventoryIndex < dragHero.heroiAtributos.slotsInventario.Length)
                    asset = dragHero.heroiAtributos.slotsInventario[dragHero.inventoryIndex];
                if (!asset) return;
                if (dragHero.heroiAtributos.RemoverAssetNoIndice(dragHero.inventoryIndex))
                {
                    InventoryManager.Instance.AddAsset(asset, 1);
                    Debug.Log($"[InvSlot] Move herói->bag asset={asset.name} slotVisual={slotIndex}");
                    dragHero.heroiAtributos.OnInventarioAlterado?.Invoke(dragHero.heroiAtributos);
                }
                return;
            }
            // Combate -> Bag
            if (dragDrop)
            {
                var asset = dragDrop.asset ? dragDrop.asset : (ScriptableObject)dragDrop.item;
                if (!asset) return;
                InventoryManager.Instance.AddAsset(asset, Mathf.Max(1, dragDrop.quantidade));
                Debug.Log($"[InvSlot] Drop combate -> bag asset={asset.name} qtd={dragDrop.quantidade}");
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
                    Debug.Log($"[InvSlot] Reordenado bag from={from} to={to} asset={temp.asset?.name}");
                    InventoryManager.Instance.RaiseInventoryChanged();
                }
                return;
            }
        }
    }
}

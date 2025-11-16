using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HeroInventoryDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public HeroiAtributos heroiAtributos;
    Image img;
    Color baseColor;

    void Awake()
    {
        img = GetComponent<Image>();
        baseColor = img ? img.color : Color.white;
        if (!heroiAtributos)
        {
            heroiAtributos = GetComponentInParent<HeroiAtributos>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (img) img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.85f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (img) img.color = baseColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (img) img.color = baseColor;
        if (!heroiAtributos)
        {
            Debug.LogWarning("[HeroDrop] HeroiAtributos não encontrado.");
            return;
        }
        var draggedGO = eventData.pointerDrag;
        if (!draggedGO) return;
        Debug.Log("[HeroDrop] OnDrop pointerDrag='" + draggedGO.name + "'");

        // Bag slot
        var bagSlot = draggedGO.GetComponent<DraggableBagSlot>();
        if (bagSlot != null && (bagSlot.asset || bagSlot.item))
        {
            // Sempre usa estado atual da bag
            ScriptableObject asset = null;
            if (InventoryManager.Instance != null && bagSlot.inventoryIndex >= 0 && bagSlot.inventoryIndex < InventoryManager.Instance.itens.Count)
                asset = InventoryManager.Instance.itens[bagSlot.inventoryIndex].asset;
            if (heroiAtributos.TentarAdicionarAsset(asset))
            {
                InventoryManager.Instance.RemoveAsset(asset, 1);
                SincronizarHerois();
                AtualizarUIs();
                Debug.Log("[HeroDrop] Item movido para herói: " + asset.name);
            }
            else
            {
                Debug.Log("[HeroDrop] Inventário do herói cheio.");
            }
            return;
        }
        else
        {
            // Drop vindo diretamente de combate (DraggableDropItem)
            var dropItem = draggedGO.GetComponent<DraggableDropItem>();
            if (dropItem != null && (dropItem.asset || dropItem.item))
            {
                var asset = dropItem.asset ? dropItem.asset : dropItem.item;
                if (heroiAtributos.TentarAdicionarAsset(asset))
                {
                    Debug.Log("[HeroDrop] Item coletado do drop diretamente para o herói: " + asset.name);
                    SincronizarHerois();
                    AtualizarUIs();
                }
                else
                {
                    Debug.Log("[HeroDrop] Inventário do herói cheio (drop direto).");
                }
                return;
            }
            else
            {
                // Transferência entre heróis
                var heroSlot = draggedGO.GetComponent<DraggableHeroInventarioSlot>();
                if (heroSlot != null && (heroSlot.asset || heroSlot.item))
                {
                    // Sempre usa estado atual do slot origem
                    ScriptableObject asset = null;
                    if (heroSlot.heroiAtributos != null && heroSlot.inventoryIndex >= 0 && heroSlot.inventoryIndex < heroSlot.heroiAtributos.slotsInventario.Length)
                        asset = heroSlot.heroiAtributos.slotsInventario[heroSlot.inventoryIndex];
                    if (heroiAtributos.TentarAdicionarAsset(asset))
                    {
                        if (heroSlot.heroiAtributos != null)
                        {
                            if (heroSlot.inventoryIndex >= 0)
                                heroSlot.heroiAtributos.RemoverAssetNoIndice(heroSlot.inventoryIndex);
                            else
                                heroSlot.heroiAtributos.RemoverAsset(asset);
                        }
                        SincronizarHerois();
                        AtualizarUIs();
                        Debug.Log("[HeroDrop] Item transferido entre heróis: " + asset.name);
                    }
                    else
                    {
                        Debug.Log("[HeroDrop] Inventário do herói destino cheio (transferência).");
                    }
                    return;
                }
            }
        }
        Debug.Log("[HeroDrop] Drop ignorado: nenhum payload reconhecido.");
    }

    void SincronizarHerois()
    {
        // Propaga slotsInventario para todas instâncias do mesmo herói (mesmo baseAtributos)
        if (!heroiAtributos) return;
        var baseRef = heroiAtributos.baseAtributos;
        if (!baseRef) return;
        var todos = FindObjectsByType<HeroiAtributos>(FindObjectsSortMode.None);
        foreach (var h in todos)
        {
            if (h == heroiAtributos) continue;
            if (h.baseAtributos == baseRef)
            {
                if (h.slotsInventario == null || h.slotsInventario.Length != heroiAtributos.slotsInventario.Length)
                    h.slotsInventario = new ScriptableObject[heroiAtributos.slotsInventario.Length];
                for (int i = 0; i < heroiAtributos.slotsInventario.Length; i++)
                {
                    h.slotsInventario[i] = heroiAtributos.slotsInventario[i];
                }
            }
        }
    }

    void AtualizarUIs()
    {
        var uisMenores = FindObjectsByType<HeroiInventarioUI>(FindObjectsSortMode.None);
        foreach (var ui in uisMenores) ui.AtualizarInventario();
        var uisMaiores = FindObjectsByType<InventarioHeroiUI>(FindObjectsSortMode.None);
        foreach (var ui in uisMaiores) ui.AtualizarInventario();
    }
}

using UnityEngine;
using UnityEngine.UI;

//Inventario Maior
public class InventarioHeroiUI : MonoBehaviour
{
    public Image iconeHeroi; // Imagem do herói no inventário
    public Transform molduraItens; // Slots dos itens
    private HeroiAtributos heroiAtributosAtual;

    public void AbrirInventario(HeroiAtributos heroiAtributos)
    {
        heroiAtributosAtual = heroiAtributos;
        Debug.Log("Abrindo inventário do herói: " + heroiAtributosAtual.name);
        AtualizarInventario();
        gameObject.SetActive(true);
        // Garante alvo de drop no ícone do herói (para receber da Bag ou de outro herói)
        if (iconeHeroi && iconeHeroi.GetComponent<HeroInventoryDropTarget>() == null)
            iconeHeroi.gameObject.AddComponent<HeroInventoryDropTarget>().heroiAtributos = heroiAtributosAtual;
        if (heroiAtributosAtual != null)
        {
            heroiAtributosAtual.OnInventarioAlterado -= OnHeroInventarioAlterado;
            heroiAtributosAtual.OnInventarioAlterado += OnHeroInventarioAlterado;
            Debug.Log("[InventarioHeroiUI] Subscrito evento inventário do herói " + heroiAtributosAtual.name + " (AbrirInventario)");
        }
    }

    public void AtualizarInventario()
    {
        if (heroiAtributosAtual == null) return;

        // Atualiza o ícone do herói
        if (iconeHeroi != null && heroiAtributosAtual.baseAtributos != null)
            iconeHeroi.sprite = heroiAtributosAtual.baseAtributos.iconeHeroi;

        // Atualiza os itens nos slots
        var slots = heroiAtributosAtual.slotsInventario;
        for (int i = 0; i < molduraItens.childCount; i++)
        {
            var slot = molduraItens.GetChild(i);
            var img = slot.GetComponentInChildren<Image>();
            if (img)
            {
                var cg = img.GetComponent<CanvasGroup>();
                if (cg && cg.blocksRaycasts == false)
                {
                    cg.blocksRaycasts = true;
                    Debug.Log("[InventarioHeroiUI] Reativando blocksRaycasts slot=" + i);
                }
                // Sempre garantir raycast para permitir drop em slots vazios
                img.raycastTarget = true;
            }
            // Garante drop target no objeto que realmente recebe raycast (imagem do slot)
            var targetGO = img ? img.gameObject : slot.gameObject;
            var dropTarget = targetGO.GetComponent<InventorySlotDropTarget>();
            if (!dropTarget)
            {
                dropTarget = targetGO.AddComponent<InventorySlotDropTarget>();
            }
            dropTarget.slotTipo = InventorySlotDropTarget.SlotTipo.Hero;
            dropTarget.heroAtributos = heroiAtributosAtual;
            dropTarget.slotIndex = i;
            if (!img) continue;
            if (slots != null && i < slots.Length && slots[i] != null)
            {
                var spr = ExtrairSprite(slots[i]);
                img.sprite = spr;
                img.enabled = spr != null;
                if (!img.enabled) img.enabled = true; // mantém imagem ativa para drop
                img.color = spr ? Color.white : new Color(1,1,1,0.05f);
                // Configura drag para este slot (inventário maior)
                var drag = img.GetComponent<DraggableHeroInventarioSlot>();
                if (!drag) drag = img.gameObject.AddComponent<DraggableHeroInventarioSlot>();
                drag.heroiAtributos = heroiAtributosAtual;
                drag.asset = slots[i];
                drag.item = slots[i] as Item;
                drag.quantidade = 1;
                drag.inventoryIndex = i;
                drag.sourceImage = img;
            }
            else
            {
                // Slot vazio: manter habilitado para aceitar drop
                img.sprite = null;
                img.enabled = true;
                img.color = new Color(1,1,1,0.05f);
                img.raycastTarget = true;
                var drag = img.GetComponent<DraggableHeroInventarioSlot>();
                if (drag) Destroy(drag);
            }
        }
    }

    void OnDestroy()
    {
        if (heroiAtributosAtual != null)
            heroiAtributosAtual.OnInventarioAlterado -= OnHeroInventarioAlterado;
    }

    void OnHeroInventarioAlterado(HeroiAtributos h)
    {
        if (h == heroiAtributosAtual)
        {
            Debug.Log("[InventarioHeroiUI] Evento inventário alterado recebido para herói " + h.name);
            AtualizarInventario();
        }
    }

    Sprite ExtrairSprite(ScriptableObject asset)
    {
        if (!asset) return null;
        var t = asset.GetType();
        var f = t.GetField("iconeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        f = t.GetField("icone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        f = t.GetField("sprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        var p = t.GetProperty("icone");
        if (p != null && p.PropertyType == typeof(Sprite)) return p.GetValue(asset, null) as Sprite;
        p = t.GetProperty("sprite");
        if (p != null && p.PropertyType == typeof(Sprite)) return p.GetValue(asset, null) as Sprite;
        return null;
    }
}
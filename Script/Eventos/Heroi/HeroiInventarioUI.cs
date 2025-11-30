using UnityEngine;
using UnityEngine.UI;

public class HeroiInventarioUI : MonoBehaviour
{
    public HeroiAtributos heroiAtributos;
    public Image iconeHeroi; // Imagem maior do inventário
    public Transform molduraItens; // Slots dos itens (HUD ou inventário maior)

    void Awake()
    {
        if (iconeHeroi == null)
            iconeHeroi = transform.Find("Personagem").GetComponent<Image>();
        if (molduraItens == null)
            molduraItens = transform.Find("Slot_Items");
    }

    void Start()
    {
        AtualizarInventario();
        // Garante alvo de drop no ícone do herói para mover itens da Bag
        if (iconeHeroi && iconeHeroi.GetComponent<HeroInventoryDropTarget>() == null)
            iconeHeroi.gameObject.AddComponent<HeroInventoryDropTarget>().heroiAtributos = heroiAtributos;
        if (heroiAtributos != null)
        {
            heroiAtributos.OnInventarioAlterado -= OnHeroInventarioAlterado; // evita duplicado
            heroiAtributos.OnInventarioAlterado += OnHeroInventarioAlterado;
            Debug.Log("[HeroiInventarioUI] Subscrito evento inventário do herói " + heroiAtributos.name + " (Start)");
        }
    }

    public void AbrirInventario()
    {
        AtualizarInventario();
        gameObject.SetActive(true);
        if (iconeHeroi && iconeHeroi.GetComponent<HeroInventoryDropTarget>() == null)
            iconeHeroi.gameObject.AddComponent<HeroInventoryDropTarget>().heroiAtributos = heroiAtributos;
        if (heroiAtributos != null)
        {
            heroiAtributos.OnInventarioAlterado -= OnHeroInventarioAlterado;
            heroiAtributos.OnInventarioAlterado += OnHeroInventarioAlterado;
            Debug.Log("[HeroiInventarioUI] Re-subscrito evento inventário (AbrirInventario) herói=" + heroiAtributos.name);
        }
    }

    public void AtualizarInventario()
    {
        // Atualiza o ícone do herói selecionado
        if (iconeHeroi != null && heroiAtributos.baseAtributos != null)
            iconeHeroi.sprite = heroiAtributos.baseAtributos.iconeHeroi;

        // Atualiza os itens nos slots
        if (heroiAtributos == null) return;
        var slots = heroiAtributos.slotsInventario;
        for (int i = 0; i < molduraItens.childCount; i++)
        {
            var slot = molduraItens.GetChild(i);
            var img = slot.GetComponentInChildren<Image>();
            // Reativa blocksRaycasts se algum drag anterior deixou false e foi destruído antes do EndDrag
            if (img)
            {
                var cg = img.GetComponent<CanvasGroup>();
                if (cg && cg.blocksRaycasts == false)
                {
                    cg.blocksRaycasts = true;
                    Debug.Log("[HeroiInventarioUI] Reativando blocksRaycasts slot=" + i);
                }
                img.raycastTarget = true; // garante drop em slot vazio
            }
            // Garante um overlay de drop quando o slot estiver vazio
            GameObject overlay = null;
            var existingOverlay = slot.Find("DropTarget");
            if (existingOverlay == null)
            {
                overlay = new GameObject("DropTarget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlay.transform.SetParent(slot, false);
                var rt = overlay.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var ovImg = overlay.GetComponent<Image>();
                ovImg.color = new Color(1,1,1,0f); // totalmente transparente
                ovImg.raycastTarget = true;
            }
            else overlay = existingOverlay.gameObject;
            if (!img) continue;
            if (slots != null && i < slots.Length && slots[i] != null)
            {
                img.sprite = ExtrairSprite(slots[i]);
                if (!img.enabled) img.enabled = true; // mantém ativo
                // Remove highlight branco: não força cor branca, mantém cor atual do Image
                // Drop target no próprio Image quando há item (para permitir swap)
                var dropTarget = img.GetComponent<InventorySlotDropTarget>();
                if (!dropTarget) dropTarget = img.gameObject.AddComponent<InventorySlotDropTarget>();
                dropTarget.slotTipo = InventorySlotDropTarget.SlotTipo.Hero;
                dropTarget.heroAtributos = heroiAtributos;
                dropTarget.slotIndex = i;
                // Impede overlay de interceptar raycasts quando há item (permite drag)
                var ovImg = overlay.GetComponent<Image>();
                if (ovImg) ovImg.raycastTarget = false;
                overlay.SetActive(false);
                // Overlay continua existindo, mas como o Image está acima, não interceptará drag
                // Configura drag para este slot
                var drag = img.GetComponent<DraggableHeroInventarioSlot>();
                if (!drag) drag = img.gameObject.AddComponent<DraggableHeroInventarioSlot>();
                drag.heroiAtributos = heroiAtributos;
                drag.asset = slots[i];
                drag.item = slots[i] as Item; // se também for Item
                drag.quantidade = 1;
                drag.inventoryIndex = i;
                drag.sourceImage = img;
            }
            else
            {
                img.sprite = null;
                img.enabled = false; // não exibe branco; drop vai para overlay
                img.raycastTarget = false;
                // Drop target no overlay (vazio)
                var ovDrop = overlay.GetComponent<InventorySlotDropTarget>();
                if (!ovDrop) ovDrop = overlay.AddComponent<InventorySlotDropTarget>();
                ovDrop.slotTipo = InventorySlotDropTarget.SlotTipo.Hero;
                ovDrop.heroAtributos = heroiAtributos;
                ovDrop.slotIndex = i;
                // Ativa overlay e garante raycast quando vazio
                overlay.SetActive(true);
                var ovImg2 = overlay.GetComponent<Image>();
                if (ovImg2) ovImg2.raycastTarget = true;
                var drag = img.GetComponent<DraggableHeroInventarioSlot>();
                if (drag) Destroy(drag);
            }
        }
    }

    void OnDestroy()
    {
        if (heroiAtributos != null)
            heroiAtributos.OnInventarioAlterado -= OnHeroInventarioAlterado;
    }

    void OnHeroInventarioAlterado(HeroiAtributos h)
    {
        if (h == heroiAtributos)
        {
            Debug.Log("[HeroiInventarioUI] Evento inventário alterado recebido para herói " + h.name);
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
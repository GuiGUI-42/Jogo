using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagInventoryUI : MonoBehaviour
{
    [Header("Root dos Slots (opcional)")] public Transform slotsRoot;
    [Header("Atualizar ao abrir a Bag")] public bool refreshOnEnable = true;

    readonly List<Image> slotImages = new();

    void Awake()
    {
        if (!slotsRoot) slotsRoot = transform; // assume filhos diretos
        ColetarSlots();
        // Assina cedo para atualizar mesmo se painel estiver oculto
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshSlotsInterno;
            InventoryManager.Instance.OnInventoryChanged += RefreshSlotsInterno;
            Debug.Log("[BagInventoryUI] Subscrito InventoryChanged (Awake)");
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshSlotsInterno; // garantir uma única subscrição
            InventoryManager.Instance.OnInventoryChanged += RefreshSlotsInterno;
        }
        if (refreshOnEnable) RefreshSlotsInterno();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshSlotsInterno;
    }

    void ColetarSlots()
    {
        slotImages.Clear();
        if (!slotsRoot) return;
        foreach (Transform t in slotsRoot)
        {
            var img = t.GetComponent<Image>();
            if (img) slotImages.Add(img);
        }
    }

    // Chamado pelo evento e por UI aberta; funciona mesmo inativo
    void RefreshSlotsInterno()
    {
        if (InventoryManager.Instance == null) return;
        var entries = InventoryManager.Instance.itens;
        // Preenche cada slot com sprite do asset correspondente; excedentes são ignorados
        for (int i = 0; i < slotImages.Count; i++)
        {
            var slotImg = slotImages[i];
            // Garante que raycasts estejam ativos (pode ter sido deixado false se drag foi destruído antes de EndDrag)
            var cg = slotImg.GetComponent<CanvasGroup>();
            if (cg && cg.blocksRaycasts == false)
            {
                cg.blocksRaycasts = true;
                Debug.Log($"[BagInventoryUI] Reativando blocksRaycasts no slot {i}");
            }
            if (i < entries.Count)
            {
                var e = entries[i];
                var spr = ExtrairSpriteDoAsset(e.asset);
                slotImg.sprite = spr;
                slotImg.color = spr ? Color.white : new Color(1,1,1,0.2f);
                slotImg.preserveAspect = true;
                AtualizarQuantidadeOverlay(slotImg.transform, e.quantidade);

                // Garante drop target por slot (bag)
                var dropTarget = slotImg.GetComponent<InventorySlotDropTarget>();
                if (!dropTarget)
                {
                    dropTarget = slotImg.gameObject.AddComponent<InventorySlotDropTarget>();
                    dropTarget.slotTipo = InventorySlotDropTarget.SlotTipo.Bag;
                    dropTarget.slotIndex = i;
                }
                else
                {
                    dropTarget.slotTipo = InventorySlotDropTarget.SlotTipo.Bag;
                    dropTarget.slotIndex = i;
                }

                // Configura drag
                var drag = slotImg.GetComponent<DraggableBagSlot>();
                if (!drag) drag = slotImg.gameObject.AddComponent<DraggableBagSlot>();
                drag.asset = e.asset;
                drag.item = e.asset as Item;
                drag.quantidade = e.quantidade;
                drag.inventoryIndex = i;
                drag.sourceImage = slotImg;
                Debug.Log($"[BagInventoryUI] Slot {i}: asset={e.asset?.name} qtd={e.quantidade} drag=ON");
            }
            else
            {
                slotImg.sprite = null;
                slotImg.color = new Color(1,1,1,0.08f);
                AtualizarQuantidadeOverlay(slotImg.transform, 0);
                var cg2 = slotImg.GetComponent<CanvasGroup>();
                if (cg2 && cg2.blocksRaycasts == false)
                {
                    cg2.blocksRaycasts = true; // mesmo vazio precisa aceitar drop
                    Debug.Log($"[BagInventoryUI] Reativando blocksRaycasts em slot vazio {i}");
                }
                var drag = slotImg.GetComponent<DraggableBagSlot>();
                if (drag) Destroy(drag);
                // Mesmo vazio ainda queremos drop target para permitir receber itens
                var dropTarget = slotImg.GetComponent<InventorySlotDropTarget>();
                if (!dropTarget)
                {
                    dropTarget = slotImg.gameObject.AddComponent<InventorySlotDropTarget>();
                    dropTarget.slotTipo = InventorySlotDropTarget.SlotTipo.Bag;
                    dropTarget.slotIndex = i;
                }
                else
                {
                    dropTarget.slotTipo = InventorySlotDropTarget.SlotTipo.Bag;
                    dropTarget.slotIndex = i;
                }
                Debug.Log($"[BagInventoryUI] Slot {i}: vazio drag=OFF");
            }
        }
    }

    // Mantém compatibilidade com antigas chamadas públicas
    public void RefreshSlots() => RefreshSlotsInterno();

    void AtualizarQuantidadeOverlay(Transform slot, int quantidade)
    {
        // procura ou cria TMP_Text de overlay
        TMP_Text txt = null;
        foreach (Transform c in slot)
        {
            txt = c.GetComponent<TMP_Text>();
            if (txt) break;
        }
        if (!txt)
        {
            var go = new GameObject("Qtd", typeof(RectTransform));
            go.transform.SetParent(slot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-4, 4);
            txt = go.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 24;
            txt.alignment = TextAlignmentOptions.BottomRight;
            // Substitui API obsoleta enableWordWrapping por textWrappingMode
            if (txt is TextMeshProUGUI tmp)
            {
                // Usa o valor correto do enum (NoWrap) para versões atuais do TMP
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
            }
            txt.color = Color.white;
            txt.outlineWidth = 0.2f;
            txt.outlineColor = Color.black;
        }
        if (quantidade > 1)
        {
            txt.gameObject.SetActive(true);
            txt.text = quantidade.ToString();
        }
        else
        {
            txt.gameObject.SetActive(false);
        }
    }

    Sprite ExtrairSpriteDoAsset(ScriptableObject asset)
    {
        if (!asset) return null;
        var t = asset.GetType();
        var f = t.GetField("icone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        f = t.GetField("iconeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
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

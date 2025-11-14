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
    }

    public void AbrirInventario()
    {
        AtualizarInventario();
        gameObject.SetActive(true);
        if (iconeHeroi && iconeHeroi.GetComponent<HeroInventoryDropTarget>() == null)
            iconeHeroi.gameObject.AddComponent<HeroInventoryDropTarget>().heroiAtributos = heroiAtributos;
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
            if (!img) continue;
            if (slots != null && i < slots.Length && slots[i] != null)
            {
                img.sprite = ExtrairSprite(slots[i]);
                img.enabled = img.sprite != null;
            }
            else
            {
                img.sprite = null;
                img.enabled = false;
            }
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
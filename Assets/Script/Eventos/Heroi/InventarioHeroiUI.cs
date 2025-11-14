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
        Debug.Log("Itens iniciais: " + heroiAtributosAtual.itensIniciais.Length);
        AtualizarInventario();
        gameObject.SetActive(true);
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
            if (!img) continue;
            if (slots != null && i < slots.Length && slots[i] != null)
            {
                var spr = ExtrairSprite(slots[i]);
                img.sprite = spr;
                img.enabled = spr != null;
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
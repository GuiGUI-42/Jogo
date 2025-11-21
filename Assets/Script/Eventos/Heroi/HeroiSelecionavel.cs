using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class HeroiSelecionavel : MonoBehaviour, IPointerClickHandler
{
    Image retrato;
    private HeroiAtributos meusAtributos;

    void Awake()
    {
        meusAtributos = GetComponent<HeroiAtributos>();
    }

    Image GetRetratoImage()
    {
        if (retrato) return retrato;
        var t = transform.Find("Personagem");
        if (t) retrato = t.GetComponent<Image>();
        if (!retrato) retrato = GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.sprite != null && i.name.ToLower().Contains("personagem"));
        if (!retrato) retrato = GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.sprite != null);
        return retrato;
    }

    public Sprite GetSpriteRetrato()
    {
        var img = GetRetratoImage();
        return img ? img.sprite : null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // --- CLIQUE DIREITO: Abrir Inventário ---
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("[HeroiSelecionavel] Clique Direito detectado: Tentando abrir inventário.");
            
            // Opção A: Tenta abrir via HUD (Recomendado se você usa o script HUDPersonagens)
            var hud = Object.FindFirstObjectByType<HUDPersonagens>(); // Unity 2023+ (use FindObjectOfType em versões antigas)
            if (hud != null)
            {
                hud.AbrirInventarioHeroi(this);
                return;
            }

            // Opção B: Tenta achar o InventarioHeroiUI diretamente (Fallback)
            var invUI = Object.FindFirstObjectByType<InventarioHeroiUI>(FindObjectsInactive.Include);
            if (invUI != null && meusAtributos != null)
            {
                invUI.AbrirInventario(meusAtributos);
            }
            else
            {
                Debug.LogWarning("Não foi possível encontrar HUDPersonagens nem InventarioHeroiUI na cena.");
            }
            return; 
        }

        // --- CLIQUE ESQUERDO: Selecionar para Evento ---
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            EventoUI eventoUI = Object.FindFirstObjectByType<EventoUI>();

            if (eventoUI != null && meusAtributos != null)
            {
                eventoUI.ReceberSelecaoHeroi(meusAtributos);
            }
            else
            {
                if (meusAtributos == null) Debug.LogWarning($"O objeto {name} não tem o componente HeroiAtributos!");
            }
        }
    }
}
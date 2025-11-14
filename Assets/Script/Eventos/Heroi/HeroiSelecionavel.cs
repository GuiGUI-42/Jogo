using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class HeroiSelecionavel : MonoBehaviour, IPointerClickHandler
{
    // pega automaticamente; nada para arrastar no Inspector
    Image retrato;

    Image GetRetratoImage()
    {
        if (retrato) return retrato;

        // 1) tenta achar o filho "Personagem"
        var t = transform.Find("Personagem");
        if (t) retrato = t.GetComponent<Image>();

        // 2) se falhar, pega um Image filho com sprite e nome sugestivo
        if (!retrato)
            retrato = GetComponentsInChildren<Image>(true)
                      .FirstOrDefault(i => i.sprite != null && i.name.ToLower().Contains("personagem"));

        // 3) fallback: qualquer Image com sprite
        if (!retrato)
            retrato = GetComponentsInChildren<Image>(true)
                      .FirstOrDefault(i => i.sprite != null);

        return retrato;
    }

    public Sprite GetSpriteRetrato()
    {
        var img = GetRetratoImage();
        return img ? img.sprite : null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            EventoUI.Instance.SelecionarHeroi(this);
        else if (eventData.button == PointerEventData.InputButton.Right)
            FindFirstObjectByType<HUDPersonagens>().AbrirInventarioHeroi(this);
    }
}
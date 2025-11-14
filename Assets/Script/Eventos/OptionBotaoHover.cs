using UnityEngine;
using UnityEngine.EventSystems;

public class OptionBotaoHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private EventoUI eventoUI;
    private EventoOpcao opcao;

    public void Configurar(EventoUI ui, EventoOpcao op)
    {
        eventoUI = ui;
        opcao = op;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventoUI != null && opcao != null)
            eventoUI.MostrarTooltipOpcao(opcao);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventoUI != null)
            eventoUI.OcultarTooltipOpcao(opcao);
    }
}

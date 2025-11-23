using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Necessário para detectar mouse over

public class BotaoOpcaoUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Interna do Botão")]
    public TextMeshProUGUI textoOpcao;
    public Image iconeOpcao; 

    private EventoOpcao dadosOpcao;
    private EventoUI manager;

    public void Configurar(EventoOpcao opcao, EventoUI uiManager)
    {
        this.dadosOpcao = opcao;
        this.manager = uiManager;

        if (textoOpcao != null) 
            textoOpcao.text = opcao.nomeOpcao;

        if (iconeOpcao != null && opcao.icone != null && opcao.usarIconeDaOpcao)
        {
            iconeOpcao.sprite = opcao.icone;
            iconeOpcao.gameObject.SetActive(true);
        }
    }

    public void AoClicar()
    {
        if (manager != null)
        {
            manager.ResolverOpcao(dadosOpcao);
        }
    }

    // --- DETECÇÃO DE MOUSE (HOVER) ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Quando o mouse entra, manda mostrar a descrição neste painel flutuante
        if (manager != null && dadosOpcao != null)
        {
            manager.MostrarDescricaoDinamica(dadosOpcao.descricao);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Quando o mouse sai, esconde ou limpa o texto
        if (manager != null)
        {
            manager.EsconderDescricaoDinamica();
        }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization; // Necessário

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

        // --- TRADUÇÃO ---
        if (textoOpcao != null) 
            textoOpcao.text = opcao.nomeOpcao.GetLocalizedString(); 

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null && dadosOpcao != null)
        {
            // --- TRADUÇÃO ---
            manager.MostrarDescricaoDinamica(dadosOpcao.descricao.GetLocalizedString());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.EsconderDescricaoDinamica();
        }
    }
}
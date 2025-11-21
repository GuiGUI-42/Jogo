using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BotaoEventoMapa : MonoBehaviour
{
    [Header("Dados")]
    public Evento eventoDados; 
    private EventoUI janelaEventoUI;

    // Estado: false = abre descrição | true = abre opções
    private bool faseOpcoes = false;

    private Image imagemButton;
    private Button componenteButton;

    void Awake()
    {
        imagemButton = GetComponent<Image>();
        componenteButton = GetComponent<Button>();
    }

    public void Configurar(Evento evento, EventoUI uiManager)
    {
        this.eventoDados = evento;
        this.janelaEventoUI = uiManager;

        if (evento.iconeEvento != null && imagemButton != null)
        {
            imagemButton.sprite = evento.iconeEvento;
        }
        
        // Garante que começa na fase inicial
        faseOpcoes = false;
    }

    public void AoClicar()
    {
        if (janelaEventoUI != null && eventoDados != null)
        {
            if (!faseOpcoes)
            {
                // FASE 1: Abre a descrição normal
                janelaEventoUI.AbrirEvento(eventoDados, this);
            }
            else
            {
                // FASE 2: Abre direto nas opções
                janelaEventoUI.AbrirTelaOpcoes(eventoDados, this);
            }
            
            // Esconde o botão do mapa
            SetVisual(false);
        }
    }

    // Chamado quando clica em "Aceitar" na UI
    public void PrepararFaseOpcoes(float tempoEspera)
    {
        faseOpcoes = true; // Muda o estado para a próxima vez
        StartCoroutine(RotinaReaparecer(tempoEspera));
    }

    // Chamado quando o evento termina totalmente (para resetar se ele reaparecer)
    public void ResetarParaInicio(float tempoEspera)
    {
        faseOpcoes = false; // Reseta o estado
        StartCoroutine(RotinaReaparecer(tempoEspera));
    }

    IEnumerator RotinaReaparecer(float tempo)
    {
        yield return new WaitForSeconds(tempo);
        SetVisual(true);
    }

    void SetVisual(bool ativo)
    {
        if (imagemButton) imagemButton.enabled = ativo;
        if (componenteButton) componenteButton.enabled = ativo;
        foreach(Transform child in transform) child.gameObject.SetActive(ativo);
    }
}
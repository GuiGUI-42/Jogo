using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BotaoEventoMapa : MonoBehaviour
{
    [Header("Dados")]
    public Evento eventoDados; 
    
    private EventoUI janelaEventoUI;
    private EventoLocal localOrigem; 
    private bool faseOpcoes = false;

    private Image imagemButton;
    private Button componenteButton;
    private Animator anim;

    void Awake()
    {
        imagemButton = GetComponent<Image>();
        componenteButton = GetComponent<Button>();
        anim = GetComponent<Animator>();

        // --- CORREÇÃO DE ANIMAÇÃO ---
        // Se existe um Animator mas não tem Controller atribuído (conforme sua imagem),
        // isso pode travar o botão se a Transição do botão estiver em "Animation".
        if (anim != null && anim.runtimeAnimatorController == null)
        {
            // Se o botão estiver configurado para usar Animation, avisa o erro
            if (componenteButton.transition == Selectable.Transition.Animation)
            {
                Debug.LogWarning($"[BotaoEventoMapa] O botão '{name}' está configurado para Animação, mas o Animator não tem Controller! O clique pode falhar.");
            }
        }
    }

    public void Configurar(Evento evento, EventoUI uiManager, EventoLocal local)
    {
        this.eventoDados = evento;
        this.janelaEventoUI = uiManager;
        this.localOrigem = local; 

        if (evento != null && evento.iconeEvento != null && imagemButton != null)
            imagemButton.sprite = evento.iconeEvento;
        
        faseOpcoes = false;
        SetVisual(true);

        // Garante que o botão está clicável ao nascer/respawnar
        if(componenteButton) componenteButton.interactable = true;
    }

    public void AoClicar()
    {
        if (janelaEventoUI != null && eventoDados != null)
        {
            if (!faseOpcoes) janelaEventoUI.AbrirEvento(eventoDados, this);
            else janelaEventoUI.AbrirTelaOpcoes(eventoDados, this);
            
            SetVisual(false); 
        }
    }

    public void PrepararFaseOpcoes(float tempoEspera)
    {
        faseOpcoes = true;
        StartCoroutine(RotinaReaparecerGarantido(tempoEspera));
    }

    public void ResetarParaInicio(float tempoEspera)
    {
        faseOpcoes = false;
        StartCoroutine(RotinaTentativaSpawn(tempoEspera));
    }

    IEnumerator RotinaReaparecerGarantido(float tempo)
    {
        yield return new WaitForSeconds(tempo);
        SetVisual(true);
    }

    IEnumerator RotinaTentativaSpawn(float tempo)
    {
        yield return new WaitForSeconds(tempo);

        if (EventoSpawner.Instance == null || !EventoSpawner.Instance.PodeSpawnarNovo())
        {
            Debug.Log($"[Botao] Limite global atingido ({EventoSpawner.Instance?.eventosFinalizados}). Removendo este ponto.");
            Destroy(gameObject); 
            yield break;
        }

        Evento novoEvento = EventoSpawner.Instance.ObterEventoAleatorio(localOrigem);

        if (novoEvento != null)
        {
            Debug.Log($"[Botao] Respawnando no local {localOrigem}: {novoEvento.nomeEvento}");
            Configurar(novoEvento, janelaEventoUI, localOrigem);
            SetVisual(true); 
        }
        else
        {
            Debug.LogWarning($"[Botao] Não há mais eventos disponíveis para {localOrigem}.");
            Destroy(gameObject);
        }
    }

    void SetVisual(bool ativo)
    {
        if (imagemButton) imagemButton.enabled = ativo;
        if (componenteButton) componenteButton.enabled = ativo;
        foreach(Transform child in transform) child.gameObject.SetActive(ativo);
    }
}
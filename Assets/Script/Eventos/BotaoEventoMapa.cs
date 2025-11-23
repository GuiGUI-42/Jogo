using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BotaoEventoMapa : MonoBehaviour
{
    [Header("Dados")]
    public Evento eventoDados; 
    private EventoUI janelaEventoUI;
    private EventoLocal localOrigem; // Armazena se é Cidade, Floresta, etc.
    private bool faseOpcoes = false;

    private Image imagemButton;
    private Button componenteButton;

    void Awake()
    {
        imagemButton = GetComponent<Image>();
        componenteButton = GetComponent<Button>();
    }

    // Atualizado para receber o local
    public void Configurar(Evento evento, EventoUI uiManager, EventoLocal local)
    {
        this.eventoDados = evento;
        this.janelaEventoUI = uiManager;
        this.localOrigem = local;

        if (evento != null && evento.iconeEvento != null && imagemButton != null)
            imagemButton.sprite = evento.iconeEvento;
        
        faseOpcoes = false;
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

    // --- NOVA LÓGICA DE RESPAWN ---
    IEnumerator RotinaTentativaSpawn(float tempo)
    {
        // 1. Espera o tempo (delay entre eventos)
        yield return new WaitForSeconds(tempo);

        // 2. Verifica se pode spawnar (limite diário)
        if (EventoSpawner.Instance == null || !EventoSpawner.Instance.PodeSpawnarNovo())
        {
            Debug.Log($"[Botao] Limite atingido ou Spawner nulo. Destruindo botão.");
            Destroy(gameObject);
            yield break;
        }

        // 3. Pede um NOVO evento aleatório para este local
        Evento novoEvento = EventoSpawner.Instance.ObterEventoAleatorio(localOrigem);

        if (novoEvento != null)
        {
            Debug.Log($"[Botao] Novo evento sorteado: {novoEvento.nomeEvento}. Reaparecendo.");
            // Reconfigura o botão com o novo evento (ícone, dados, etc)
            Configurar(novoEvento, janelaEventoUI, localOrigem);
            SetVisual(true);
        }
        else
        {
            // Se não há mais eventos disponíveis na lista, removemos o botão
            Debug.LogWarning("[Botao] Não há eventos disponíveis para este local.");
            Destroy(gameObject);
        }
    }

    void SetVisual(bool ativo)
    {
        if (imagemButton) imagemButton.enabled = ativo;
        if (componenteButton) componenteButton.enabled = ativo;
        // Ativa/Desativa filhos (como textos ou partículas anexadas)
        foreach(Transform child in transform) child.gameObject.SetActive(ativo);
    }
}
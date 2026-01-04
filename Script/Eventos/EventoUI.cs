using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;

public class EventoUI : MonoBehaviour
{
    [Header("Elementos da Tela 1 (Descricao)")]
    public GameObject painelPrincipal;
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoDescricao;
    public Image imagemEvento;
    public GameObject botaoAceiteObjeto;

    [Header("Seleção de Herói")]
    public Image slotHeroiImagem; 
    public Sprite spriteSlotVazio; 
    public HeroiAtributos heroiParticipante { get; private set; }
    public Transform containerAjudantes; 
    public GameObject prefabSlotAjudante;

    [Header("Dinâmica")]
    public GameObject dinamicaEventoPainel; 
    public TextMeshProUGUI textoDinamica;   

    [Header("Sistemas de Resolução")]
    public EventoCombateUI combateUI; 
    public EventoPassivoUI passivoUI; 
    public EventoLojaUI lojaUI; // <-- ARRASTE O SCRIPT DA LOJA AQUI NO INSPECTOR

    [Header("Opções (Botões)")]
    public Transform containerOpcoes;
    public GameObject prefabOpcaoCombate;
    public GameObject prefabOpcaoPassivo;
    public GameObject prefabOpcaoLoja;
    public GameObject prefabOpcaoOuro;

    private Evento eventoAtual;
    private BotaoEventoMapa botaoOrigem;

    void Start()
    {
        if(painelPrincipal) painelPrincipal.SetActive(false);
        if(dinamicaEventoPainel) dinamicaEventoPainel.SetActive(false);
        if(containerAjudantes) containerAjudantes.gameObject.SetActive(false);
    }

    // FASE 1: Abertura
    public void AbrirEvento(Evento evento, BotaoEventoMapa origem)
    {
        this.eventoAtual = evento;
        this.botaoOrigem = origem;
        this.heroiParticipante = null;
        
        AtualizarSlotVisual();
        ConfigurarSlotsExtras(); 
        
        if(textoTitulo) textoTitulo.text = evento.nomeEvento.GetLocalizedString();
        if(textoDescricao) textoDescricao.text = evento.descricaoEvento.GetLocalizedString();
        if(imagemEvento && evento.iconeEvento) imagemEvento.sprite = evento.iconeEvento;
        
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(true);
        if(dinamicaEventoPainel) dinamicaEventoPainel.SetActive(false);

        LimparBotoesAntigos();
        
        if(painelPrincipal) painelPrincipal.SetActive(true);
    }

    public void ReceberSelecaoHeroi(HeroiAtributos heroi)
    {
        if (!painelPrincipal.activeSelf || (botaoAceiteObjeto != null && !botaoAceiteObjeto.activeSelf)) return;
        this.heroiParticipante = heroi;
        AtualizarSlotVisual();
    }

    void AtualizarSlotVisual()
    {
        if (slotHeroiImagem == null) return;
        if (heroiParticipante != null && heroiParticipante.baseAtributos != null)
        {
            slotHeroiImagem.sprite = heroiParticipante.baseAtributos.iconeHeroi;
            slotHeroiImagem.color = Color.white; 
            slotHeroiImagem.enabled = true;
        }
        else
        {
            if (spriteSlotVazio != null) { slotHeroiImagem.sprite = spriteSlotVazio; slotHeroiImagem.enabled = true; }
            else { slotHeroiImagem.color = Color.clear; }
        }
    }

    void ConfigurarSlotsExtras()
    {
        if (containerAjudantes == null || prefabSlotAjudante == null || eventoAtual == null) return;
        foreach(Transform child in containerAjudantes) Destroy(child.gameObject);

        int slotsExtras = (int)eventoAtual.slotsNecessarios - 1;
        if (slotsExtras > 0)
        {
            containerAjudantes.gameObject.SetActive(true);
            for (int i = 0; i < slotsExtras; i++) Instantiate(prefabSlotAjudante, containerAjudantes);
        }
        else containerAjudantes.gameObject.SetActive(false);
    }

    public void BotaoAceitar()
    {
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(false);
        if (botaoOrigem != null) botaoOrigem.PrepararFaseOpcoes(2f);
        painelPrincipal.SetActive(false); 
    }

    // FASE 2: Opções
    public void AbrirTelaOpcoes(Evento evento, BotaoEventoMapa origem)
    {
        this.eventoAtual = evento;
        this.botaoOrigem = origem;

        if(textoTitulo) textoTitulo.text = evento.nomeEvento.GetLocalizedString();
        if(textoDescricao) textoDescricao.text = evento.descricaoEvento.GetLocalizedString();
        if(imagemEvento && evento.iconeEvento) imagemEvento.sprite = evento.iconeEvento;
        
        painelPrincipal.SetActive(true);
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(false);
        
        if(dinamicaEventoPainel) 
        {
            dinamicaEventoPainel.SetActive(true);
            if(textoDinamica) textoDinamica.text = "Escolha uma opção..."; 
        }

        AtualizarSlotVisual();
        ConfigurarSlotsExtras(); 
        LimparBotoesAntigos();
        GerarOpcoes();
    }

    void GerarOpcoes()
    {
        if (eventoAtual == null || containerOpcoes == null) return;
        foreach (var opcao in eventoAtual.opcoesDecisao)
        {
            GameObject prefabUsar = null;
            switch (opcao.tipo)
            {
                case TipoEvento.Combate: prefabUsar = prefabOpcaoCombate; break;
                case TipoEvento.Passivo: prefabUsar = prefabOpcaoPassivo; break;
                case TipoEvento.Loja:    prefabUsar = prefabOpcaoLoja; break;
                case TipoEvento.Ouro:    prefabUsar = prefabOpcaoOuro; break;
            }
            if(prefabUsar == null) prefabUsar = prefabOpcaoCombate; 

            if (prefabUsar != null)
            {
                GameObject btn = Instantiate(prefabUsar, containerOpcoes);
                var scriptBtn = btn.GetComponent<BotaoOpcaoUI>();
                if (scriptBtn) scriptBtn.Configurar(opcao, this);
            }
        }
    }

    public void ResolverOpcao(EventoOpcao opcaoEscolhida)
    {
        painelPrincipal.SetActive(false);
        if(dinamicaEventoPainel) dinamicaEventoPainel.SetActive(false);

        switch (opcaoEscolhida.tipo)
        {
            case TipoEvento.Combate:
                if (combateUI) combateUI.IniciarCombate(heroiParticipante, eventoAtual.monstroPrefab, opcaoEscolhida);
                else FinalizarCicloDoEvento(false);
                break;

            case TipoEvento.Passivo:
                if (passivoUI) passivoUI.ResolverPassivo(heroiParticipante, opcaoEscolhida);
                else FinalizarCicloDoEvento(false); 
                break;

            case TipoEvento.Loja:
                // --- ABRE A LOJA ---
                if (lojaUI)
                {
                    lojaUI.AbrirLoja(opcaoEscolhida, this);
                }
                else
                {
                    Debug.LogError("[EventoUI] Faltando referência da LojaUI!");
                    FinalizarCicloDoEvento(false);
                }
                break;

            case TipoEvento.Ouro:
                // --- PAGA OURO DIRETO (SUBORNO) ---
                if (GameManager.instance.TemOuroSuficiente(opcaoEscolhida.custoOuro))
                {
                    GameManager.instance.GastarOuro(opcaoEscolhida.custoOuro);
                    Debug.Log($"[EventoUI] Pagou {opcaoEscolhida.custoOuro} de Ouro. Sucesso!");
                    FinalizarCicloDoEvento(true); 
                }
                else
                {
                    Debug.Log("[EventoUI] Ouro insuficiente para esta opção.");
                    FinalizarCicloDoEvento(false); // Falha
                }
                break;
        }
    }

    public void FinalizarCicloDoEvento(bool sucesso)
    {
        if (EventoSpawner.Instance != null && eventoAtual != null)
            EventoSpawner.Instance.RegistrarEventoFinalizado(eventoAtual, sucesso);

        // O GameManager atualiza a UI de recompensas finais
        if (GameManager.instance != null && eventoAtual != null)
            GameManager.instance.FinalizarEvento(sucesso, eventoAtual.recompensaOuro, eventoAtual.recompensaReputacao);

        if(painelPrincipal) painelPrincipal.SetActive(false);

        if (botaoOrigem != null)
        {
            botaoOrigem.ResetarParaInicio(2f); 
            botaoOrigem = null;
        }
        eventoAtual = null;
    } 

    public void MostrarDescricaoDinamica(string descricao) { if (textoDinamica) textoDinamica.text = descricao; }
    public void EsconderDescricaoDinamica() { if (textoDinamica) textoDinamica.text = "Escolha uma opção..."; }
    void LimparBotoesAntigos() { foreach (Transform child in containerOpcoes) Destroy(child.gameObject); }
}
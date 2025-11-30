using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EventoUI : MonoBehaviour
{
    [Header("Elementos da Tela 1 (Descricao)")]
    public GameObject painelPrincipal;
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoDescricao;
    public Image imagemEvento;
    public GameObject botaoAceiteObjeto;

    [Header("Seleção de Herói Principal")]
    public Image slotHeroiImagem; 
    public Sprite spriteSlotVazio; 
    public HeroiAtributos heroiParticipante { get; private set; }

    [Header("Multiplos Herois (Visual)")]
    [Tooltip("Arraste o objeto Container (com Horizontal Layout Group) que ficará dentro ou ao lado do Slot_Heroi.")]
    public Transform containerAjudantes; 
    [Tooltip("Arraste um Prefab simples de uma imagem/moldura vazia para representar o slot do ajudante.")]
    public GameObject prefabSlotAjudante;

    [Header("Painel Dinâmica (Hover das Opções)")]
    public GameObject dinamicaEventoPainel; 
    public TextMeshProUGUI textoDinamica;   

    [Header("Sistemas de Resolução")]
    public EventoCombateUI combateUI; 
    public EventoPassivoUI passivoUI; 

    [Header("Elementos da Tela 2 (Opções)")]
    public Transform containerOpcoes;
    public GameObject prefabOpcaoCombate;
    public GameObject prefabOpcaoPassivo;

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
        
        if(textoTitulo) textoTitulo.text = evento.nomeEvento;
        if(textoDescricao) textoDescricao.text = evento.descricaoEvento;
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

        int totalSlots = (int)eventoAtual.slotsNecessarios;
        int slotsExtras = totalSlots - 1;

        if (slotsExtras > 0)
        {
            containerAjudantes.gameObject.SetActive(true);
            for (int i = 0; i < slotsExtras; i++)
            {
                Instantiate(prefabSlotAjudante, containerAjudantes);
            }
        }
        else
        {
            containerAjudantes.gameObject.SetActive(false);
        }
    }

    public void BotaoAceitar()
    {
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(false);
        if (botaoOrigem != null) botaoOrigem.PrepararFaseOpcoes(2f);
        if(painelPrincipal) painelPrincipal.SetActive(false);
    }

    // FASE 2: Opções
    public void AbrirTelaOpcoes(Evento evento, BotaoEventoMapa origem)
    {
        this.eventoAtual = evento;
        this.botaoOrigem = origem;

        if(textoTitulo) textoTitulo.text = evento.nomeEvento;
        if(textoDescricao) textoDescricao.text = evento.descricaoEvento;
        if(imagemEvento && evento.iconeEvento) imagemEvento.sprite = evento.iconeEvento;
        
        if(painelPrincipal) painelPrincipal.SetActive(true);
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
            GameObject prefabUsar = (opcao.tipo == TipoEvento.Combate) ? prefabOpcaoCombate : prefabOpcaoPassivo;
            if (prefabUsar != null)
            {
                GameObject btn = Instantiate(prefabUsar, containerOpcoes);
                var scriptBtn = btn.GetComponent<BotaoOpcaoUI>();
                if (scriptBtn) scriptBtn.Configurar(opcao, this);
            }
        }
    }

    public void MostrarDescricaoDinamica(string descricao)
    {
        if (dinamicaEventoPainel && textoDinamica) textoDinamica.text = descricao;
    }

    public void EsconderDescricaoDinamica()
    {
        if (dinamicaEventoPainel && textoDinamica) textoDinamica.text = "Escolha uma opção...";
    }

    public void ResolverOpcao(EventoOpcao opcaoEscolhida)
    {
        Debug.Log($"Jogador escolheu: {opcaoEscolhida.nomeOpcao} ({opcaoEscolhida.tipo})");

        painelPrincipal.SetActive(false);
        if(dinamicaEventoPainel) dinamicaEventoPainel.SetActive(false);

        if (opcaoEscolhida.tipo == TipoEvento.Combate)
        {
            if (combateUI != null && heroiParticipante != null && eventoAtual.monstroPrefab != null)
            {
                combateUI.IniciarCombate(heroiParticipante, eventoAtual.monstroPrefab, opcaoEscolhida);
            }
            else
            {
                Debug.LogError("Erro ao iniciar combate! Verifique referências no EventoUI.");
                // Se falhou ao iniciar, consideramos falha no evento
                FinalizarCicloDoEvento(false); 
            }
        }
        else 
        {
            if (passivoUI != null && heroiParticipante != null)
            {
                passivoUI.ResolverPassivo(heroiParticipante, opcaoEscolhida);
            }
            else
            {
                Debug.LogError("Erro Passivo: Faltando PassivoUI ou Herói Selecionado!");
                FinalizarCicloDoEvento(false); 
            }
        }
    }

    // --- ASSINATURA ALTERADA PARA RECEBER RESULTADO ---
    public void FinalizarCicloDoEvento(bool sucesso)
    {
        Debug.Log($"Finalizando Ciclo do Evento. Sucesso: {sucesso}");

        if (EventoSpawner.Instance != null && eventoAtual != null)
        {
            // Informa ao Spawner qual evento terminou e se foi bem sucedido
            EventoSpawner.Instance.RegistrarEventoFinalizado(eventoAtual, sucesso);
        }

        if(painelPrincipal) painelPrincipal.SetActive(false);

        if (botaoOrigem != null)
        {
            botaoOrigem.ResetarParaInicio(2f); 
            botaoOrigem = null;
        }
        
        // Limpa referência para evitar lixo
        eventoAtual = null;
    } 

    void LimparBotoesAntigos()
    {
        if (!containerOpcoes) return;
        foreach (Transform child in containerOpcoes) Destroy(child.gameObject);
    }
}
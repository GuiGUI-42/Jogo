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

    [Header("Seleção de Herói")]
    public Image slotHeroiImagem; 
    public Sprite spriteSlotVazio; 
    public HeroiAtributos heroiParticipante { get; private set; }

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
    }

    // FASE 1: Abertura
    public void AbrirEvento(Evento evento, BotaoEventoMapa origem)
    {
        this.eventoAtual = evento;
        this.botaoOrigem = origem;
        this.heroiParticipante = null;
        
        AtualizarSlotVisual();
        
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
        
        if(painelPrincipal) painelPrincipal.SetActive(true);
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(false);
        
        if(dinamicaEventoPainel) 
        {
            dinamicaEventoPainel.SetActive(true);
            if(textoDinamica) textoDinamica.text = "Escolha uma opção..."; 
        }

        AtualizarSlotVisual();
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
                FinalizarCicloDoEvento(); // Fecha para não travar
            }
        }
    }

    // --- ESTE MÉTODO É O CORAÇÃO DO LOOP ---
    public void FinalizarCicloDoEvento()
    {
        Debug.Log("Finalizando Ciclo do Evento.");

        // 1. Avisa o Spawner para contar +1 evento finalizado
        if (EventoSpawner.Instance != null)
        {
            EventoSpawner.Instance.RegistrarEventoFinalizado();
        }

        // 2. Fecha janela principal se estiver aberta
        if(painelPrincipal) painelPrincipal.SetActive(false);

        // 3. Manda o botão original tentar spawnar de novo (com 50% chance e delay)
        if (botaoOrigem != null)
        {
            botaoOrigem.ResetarParaInicio(2f); 
            botaoOrigem = null;
        }
    } 

    void LimparBotoesAntigos()
    {
        if (!containerOpcoes) return;
        foreach (Transform child in containerOpcoes) Destroy(child.gameObject);
    }
}
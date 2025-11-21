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
    public Image slotHeroiImagem; // ARRASTE O "Heroi_Slot" AQUI
    public Sprite spriteSlotVazio; // Opcional: Sprite padrão quando ninguém está selecionado
    
    // Esta variável guarda quem foi selecionado para usar nas opções depois
    public HeroiAtributos heroiParticipante { get; private set; }

    [Header("Sistema de Combate")]
    public EventoCombateUI combateUI; // ARRASTE O OBJETO DO COMBATE AQUI

    [Header("Elementos da Tela 2 (Opções)")]
    public Transform containerOpcoes;
    public GameObject prefabOpcaoCombate;
    public GameObject prefabOpcaoPassivo;

    // Estado interno
    private Evento eventoAtual;
    private BotaoEventoMapa botaoOrigem;

    void Start()
    {
        if(painelPrincipal) painelPrincipal.SetActive(false);
    }

    // FASE 1: Abertura
    public void AbrirEvento(Evento evento, BotaoEventoMapa origem)
    {
        this.eventoAtual = evento;
        this.botaoOrigem = origem;
        
        // Reseta o herói selecionado ao abrir um novo evento
        this.heroiParticipante = null;
        AtualizarSlotVisual();

        // Configura visual
        if(textoTitulo) textoTitulo.text = evento.nomeEvento;
        if(textoDescricao) textoDescricao.text = evento.descricaoEvento;
        if(imagemEvento && evento.iconeEvento) imagemEvento.sprite = evento.iconeEvento;

        // Mostra botão aceite
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(true);
        LimparBotoesAntigos();

        if(painelPrincipal) painelPrincipal.SetActive(true);
    }

    // --- Lógica de Seleção de Herói ---
    public void ReceberSelecaoHeroi(HeroiAtributos heroi)
    {
        // Só permite selecionar se a janela estiver aberta e na Fase 1 (Botão Aceite ativo)
        if (!painelPrincipal.activeSelf || (botaoAceiteObjeto != null && !botaoAceiteObjeto.activeSelf))
            return;

        this.heroiParticipante = heroi;
        Debug.Log($"[EventoUI] Herói selecionado: {heroi.name}");
        
        AtualizarSlotVisual();
    }

    void AtualizarSlotVisual()
    {
        if (slotHeroiImagem == null) return;

        if (heroiParticipante != null && heroiParticipante.baseAtributos != null)
        {
            // Mostra a foto do herói
            slotHeroiImagem.sprite = heroiParticipante.baseAtributos.iconeHeroi;
            
            // Garante que a imagem está visível e branca (sem tintura escura)
            slotHeroiImagem.color = Color.white; 
            slotHeroiImagem.enabled = true;
        }
        else
        {
            // Se não tem herói, mostra vazio ou esconde
            if (spriteSlotVazio != null)
            {
                slotHeroiImagem.sprite = spriteSlotVazio;
                slotHeroiImagem.enabled = true;
            }
            else
            {
                slotHeroiImagem.color = Color.clear; 
            }
        }
    }
    // ---------------------------------------

    public void BotaoAceitar()
    {
        // Opcional: Se quiser obrigar a seleção, descomente abaixo:
        /*
        if (heroiParticipante == null) {
            Debug.LogWarning("Selecione um herói antes de aceitar!");
            return;
        }
        */

        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(false);
        
        // O botão do mapa vai esperar 2s
        if (botaoOrigem != null)
        {
            botaoOrigem.PrepararFaseOpcoes(2f);
        }
        
        if(painelPrincipal) painelPrincipal.SetActive(false);
    }

    // FASE 2: Opções
    public void AbrirTelaOpcoes(Evento evento, BotaoEventoMapa origem)
    {
        this.eventoAtual = evento;
        this.botaoOrigem = origem;

        if(painelPrincipal) painelPrincipal.SetActive(true);
        if(botaoAceiteObjeto) botaoAceiteObjeto.SetActive(false);
        
        // Garante que a foto do herói continue aparecendo na fase 2
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

    public void ResolverOpcao(EventoOpcao opcaoEscolhida)
    {
        Debug.Log($"Jogador escolheu: {opcaoEscolhida.nomeOpcao} ({opcaoEscolhida.tipo})");

        if (opcaoEscolhida.tipo == TipoEvento.Combate)
        {
            if (combateUI != null && heroiParticipante != null && eventoAtual.monstroPrefab != null)
            {
                painelPrincipal.SetActive(false);
                
                // CHANGE: Passando "opcaoEscolhida" como 3º parâmetro
                combateUI.IniciarCombate(heroiParticipante, eventoAtual.monstroPrefab, opcaoEscolhida);
            }
            else
            {
                Debug.LogError("Erro ao iniciar combate: Faltando UI, Herói ou Prefab do Monstro!");
            }
        }
        else
        {
            // Lógica Passiva
            FecharEAgendarRetorno();
            heroiParticipante = null;
        }
    }

    // ESTA ERA A FUNÇÃO QUE FALTAVA:
    void FecharEAgendarRetorno()
    {
        // Fecha a janela visualmente
        if(painelPrincipal) painelPrincipal.SetActive(false);

        // Avisa o botão do mapa para resetar o ciclo e voltar daqui a 2 segundos
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
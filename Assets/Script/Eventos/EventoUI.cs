using System.Collections;
using System; // Guid
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventoUI : MonoBehaviour
{
    public static EventoUI Instance;
    // Dispara quando um evento é totalmente concluído (após combate ou passivo).
    public static event Action<EventoLocal, Evento> OnEventoResolvido;

    // Fases de visualização do fluxo do evento
    // IntroFiltrada: primeira abertura (ícone) mostrando apenas Título, Descrição, Slot Herói e Botão Aceite
    // Intro: versão completa da introdução (sem opções ainda)
    // Opcoes: botões de decisão visíveis
    enum FaseVisualEvento { IntroFiltrada, Intro, Opcoes }
    FaseVisualEvento faseAtual = FaseVisualEvento.IntroFiltrada;

    public GameObject painelEvento;          // apenas a janela do evento
    public Transform botoesContainer;        // onde os botões de opções serão instanciados
    public GameObject botaoCombatePrefab;    // prefab para botão de opção de combate
    public GameObject botaoPassivoPrefab;    // prefab para botão de opção passiva
    public TMP_Text nomeEventoText;
    public TMP_Text descricaoEventoText;
    [Header("Tooltip Opção")]
    public GameObject painelTooltipOpcao;    // painel para mostrar descrição da opção (hover)
    public TMP_Text tooltipTituloText;
    public TMP_Text tooltipDescricaoText;
    public Image slotHeroiImage;
    public HeroiSelecionavel heroiSelecionado;

    public EventoCombateUI combateUI;
    public PassivoUI passivoUI;
    Evento eventoAtual;
    EventoOpcao opcaoSelecionada; // opção escolhida pelo jogador
    readonly List<Button> botoesGerados = new List<Button>();

    public Button botaoAceite; // Adicionado para depuração

    // Removidos: campos de respawn/reabrir ícone (origem no mundo, prefab e snapshots)

    [Header("Reabrir")]
    [Tooltip("Atraso (segundos) para mostrar o ícone de reabrir após aceitar.")]
    public float delayReabrirSeg = 3f;

    // Dados capturados da origem (ícone inicial) para respawn do ícone de reabrir
    GameObject ultimoRespawnPrefab;
    Transform ultimoRespawnParent;
    Vector3 ultimoRespawnPos;
    Quaternion ultimoRespawnRot = Quaternion.identity;
    // ID único por instância de evento aberto via ícone
    string eventoInstanceId;
    // Referência ao ícone original desta instância
    GameObject origemIconeAtual;

    void Awake()
    {
        Instance = this;
        if (painelEvento == null) painelEvento = gameObject;
        if (combateUI == null) combateUI = UnityEngine.Object.FindFirstObjectByType<EventoCombateUI>(FindObjectsInactive.Include);
        if (passivoUI == null) passivoUI = UnityEngine.Object.FindFirstObjectByType<PassivoUI>(FindObjectsInactive.Include);
        if (painelEvento != null)
        {
            // Se o painel é o próprio root, não desative o GameObject para permitir reabertura
            if (painelEvento == this.gameObject)
                SetPainelEventoVisivel(false);
            else
                painelEvento.SetActive(false);
        }

        if (botoesContainer == null && painelEvento != null)
            botoesContainer = painelEvento.transform;
        // Se o usuário arrastou um prefab (asset) em vez de um objeto da cena, substitui pelo painel
        if (botoesContainer != null && !botoesContainer.gameObject.scene.IsValid())
        {
            Debug.LogWarning("[EventoUI] 'BotoesContainer' aponta para um prefab asset. Usando painelEvento como container.");
            botoesContainer = painelEvento != null ? painelEvento.transform : transform;
        }

        // Botão legado desativado; opções serão geradas dinamicamente
        if (botaoAceite != null)
        {
            botaoAceite.gameObject.SetActive(false);
            botaoAceite.onClick.RemoveAllListeners();
            botaoAceite.onClick.AddListener(OnClickAceite);
        }

        // Oculta tooltip inicial
        if (painelTooltipOpcao != null) painelTooltipOpcao.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Mantido para compatibilidade com quem chama EventoUI.EnsureInstance()
    public static EventoUI EnsureInstance()
    {
        if (Instance == null)
            Instance = UnityEngine.Object.FindFirstObjectByType<EventoUI>(FindObjectsInactive.Include);
        return Instance;
    }

    public void Abrir(Evento evento)
    {
        Debug.Log($"[EventoUI] Abrir chamado com evento='{evento?.name}', faseAtual={faseAtual}");
        // Sempre inicia com a versão filtrada na primeira chamada
        AbrirIntroFiltrada(evento);
    }

    // Abre o evento vindo de um ícone específico, gerando ID e registrando origem
    public void AbrirDoIcone(Evento evento, GameObject origemIcone)
    {
        eventoInstanceId = Guid.NewGuid().ToString("N");
        origemIconeAtual = origemIcone;
        if (origemIcone != null)
        {
            // Registra dados de respawn a partir do componente existente
            DefinirUltimaOrigemIcone(origemIcone.transform, origemIcone, evento);
        }
        Debug.Log($"[EventoUI] AbrirDoIcone ID={eventoInstanceId} origem='{origemIcone?.name}' evento='{evento?.name}'");
        Abrir(evento);
    }

    void AbrirIntroFiltrada(Evento evento)
    {
        faseAtual = FaseVisualEvento.IntroFiltrada;
        eventoAtual = evento;
        opcaoSelecionada = null;
        if (painelEvento != null)
        {
            // Se o root foi desativado por ESC, reativa antes de aplicar visibilidade interna
            if (painelEvento == this.gameObject && !gameObject.activeSelf)
                gameObject.SetActive(true);
            SetPainelEventoVisivel(true);
            FiltrarLayoutIntro();
        }
        if (nomeEventoText != null) nomeEventoText.text = evento != null ? evento.nomeEvento : "";
        if (descricaoEventoText != null) descricaoEventoText.text = evento != null ? evento.descricaoEvento : "";

        LimparBotoes();
        if (botaoAceite != null)
        {
            botaoAceite.gameObject.SetActive(true);
            botaoAceite.interactable = true; // reset independente por evento
        }
        DebugChildrenEstado();
    }

    void AbrirIntroFull(Evento evento)
    {
        faseAtual = FaseVisualEvento.Intro;
        eventoAtual = evento;
        // Removido: associação de origem do evento para respawn
        opcaoSelecionada = null;
        if (painelEvento != null)
        {
            if (painelEvento == this.gameObject && !gameObject.activeSelf)
                gameObject.SetActive(true);
            SetPainelEventoVisivel(true);
            RestaurarLayoutEvento();
        }
        if (nomeEventoText != null) nomeEventoText.text = evento != null ? evento.nomeEvento : "";
        if (descricaoEventoText != null) descricaoEventoText.text = evento != null ? evento.descricaoEvento : "";

        // Não gerar botões ainda; apenas garantir que antigos sejam removidos
        LimparBotoes();

        // Mostrar botão de aceite para avançar
        if (botaoAceite != null)
        {
            botaoAceite.gameObject.SetActive(true);
            botaoAceite.interactable = true; // reset independente
        }

        DebugChildrenEstado();
    }

    void LimparBotoes()
    {
        foreach (var b in botoesGerados) if (b) Destroy(b.gameObject);
        botoesGerados.Clear();
    }

    public void Fechar()
    {
        if (painelEvento != null)
        {
            if (painelEvento == this.gameObject)
                SetPainelEventoVisivel(false);
            else
                painelEvento.SetActive(false);
        }
    }

    public void SelecionarHeroi(HeroiSelecionavel heroi)
    {
        heroiSelecionado = heroi;
        var sprite = heroi != null ? heroi.GetSpriteRetrato() : null;
        if (sprite == null) return;

        if (slotHeroiImage != null)
        {
            slotHeroiImage.sprite = sprite;
            slotHeroiImage.preserveAspect = true;
            slotHeroiImage.color = Color.white;
        }
    }

    // Chamar este método quando jogador selecionar uma opção
    public void SelecionarOpcao(int indice)
    {
        opcaoSelecionada = eventoAtual?.ObterOpcao(indice);
        Debug.Log("[EventoUI] Opção selecionada: " + (opcaoSelecionada != null ? opcaoSelecionada.nomeOpcao : "(nula)"));
    }

    void GerarBotoesDeOpcoes()
    {
        if (faseAtual != FaseVisualEvento.Opcoes) return; // Garante que só gera na fase correta
        // Limpa anteriores
        LimparBotoes();

        if (botoesContainer != null && !botoesContainer.gameObject.scene.IsValid())
        {
            Debug.LogWarning("[EventoUI] Container ainda é um prefab asset. Reatribuindo.");
            botoesContainer = painelEvento != null ? painelEvento.transform : transform;
        }
        if (botoesContainer == null || eventoAtual == null || eventoAtual.opcoesDecisao == null) return;

        for (int i = 0; i < eventoAtual.opcoesDecisao.Length; i++)
        {
            var opc = eventoAtual.opcoesDecisao[i];
            if (opc == null) continue;

            GameObject prefab = opc.tipo == TipoEvento.Combate ? botaoCombatePrefab : botaoPassivoPrefab;
            string label = string.IsNullOrEmpty(opc.nomeOpcao) ? (opc.tipo == TipoEvento.Combate ? "Iniciar Combate" : "Opção Passiva") : opc.nomeOpcao;
            var iconToUse = (opc.usarIconeDaOpcao ? opc.icone : null);
            var botao = InstanciarBotao(prefab, label, iconToUse);
            if (botao == null) continue;

            int indiceLocal = i; var opcLocal = opc;
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(() =>
            {
                SelecionarOpcao(indiceLocal);
                if (opcLocal.tipo == TipoEvento.Combate) AbrirCombate();
                else AbrirPassivo();
            });

            // Adiciona comportamento de hover para tooltip
            var hover = botao.gameObject.GetComponent<OptionBotaoHover>();
            if (hover == null) hover = botao.gameObject.AddComponent<OptionBotaoHover>();
            hover.Configurar(this, opcLocal);

            botoesGerados.Add(botao);
        }
    }

    Button InstanciarBotao(GameObject prefab, string label, Sprite icon)
    {
        Button btn = null;
        if (prefab != null)
        {
            // Instancia sem parent primeiro para evitar erro de parent persistente
            var go = Instantiate(prefab);
            // Só faz SetParent se container for objeto da cena
            if (botoesContainer != null && botoesContainer.gameObject.scene.IsValid())
                go.transform.SetParent(botoesContainer, false);
            else
                Debug.LogWarning("[EventoUI] Botão instanciado sem parent válido; usando root do EventoUI.");
            btn = go.GetComponent<Button>();
            // Aplica ícone se fornecido
            if (icon != null)
            {
                var img = go.GetComponent<Image>();
                if (img == null) img = go.GetComponentInChildren<Image>();
                if (img != null) { img.sprite = icon; img.preserveAspect = true; }
            }
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = label;
        }
        else
        {
            // Cria um botão simples se nenhum prefab foi fornecido
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            if (botoesContainer != null && botoesContainer.gameObject.scene.IsValid())
                rt.SetParent(botoesContainer, false);
            else
                rt.SetParent(transform, false);
            rt.sizeDelta = new Vector2(180, 40);
            btn = go.GetComponent<Button>();
            // Define sprite se houver ícone
            if (icon != null)
            {
                var img = go.GetComponent<Image>();
                if (img != null) { img.sprite = icon; img.preserveAspect = true; }
            }
            var textGO = new GameObject("Label", typeof(RectTransform), typeof(TMP_Text));
            var rtText = (RectTransform)textGO.transform;
            rtText.SetParent(go.transform, false);
            rtText.anchorMin = Vector2.zero; rtText.anchorMax = Vector2.one; rtText.offsetMin = Vector2.zero; rtText.offsetMax = Vector2.zero;
            var tmp = textGO.GetComponent<TMP_Text>();
            tmp.text = label; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.black;
        }
        return btn;
    }

    // Tooltip: mostra descrição da opção ao passar o mouse
    public void MostrarTooltipOpcao(EventoOpcao opc)
    {
        if (painelTooltipOpcao == null || opc == null) return;
        painelTooltipOpcao.SetActive(true);
        if (tooltipTituloText != null) tooltipTituloText.text = string.IsNullOrEmpty(opc.nomeOpcao) ? eventoAtual?.nomeEvento : opc.nomeOpcao;
        if (tooltipDescricaoText != null) tooltipDescricaoText.text = opc.descricao;
    }

    public void OcultarTooltipOpcao(EventoOpcao opc)
    {
        if (painelTooltipOpcao == null) return;
        painelTooltipOpcao.SetActive(false);
    }

    public void AbrirCombate()
    {
        Debug.Log("[EventoUI] AbrirCombate chamado pelo botão!");
        if (combateUI == null) combateUI = UnityEngine.Object.FindFirstObjectByType<EventoCombateUI>(FindObjectsInactive.Include);
        if (combateUI == null) { Debug.LogError("EventoCombateUI não encontrado na cena."); return; }
        if (eventoAtual == null) { Debug.LogError("Nenhum evento atual."); return; }

        // Verifica se a opção selecionada é de combate
        if (opcaoSelecionada == null || opcaoSelecionada.tipo != TipoEvento.Combate)
        {
            Debug.LogWarning("[EventoUI] AbrirCombate chamado sem opção de combate selecionada.");
            return;
        }

        var monstroGO = eventoAtual.monstroPrefab; // TODO: se cada opção tiver prefab específico, mover para EventoOpcao
        if (monstroGO == null)
        {
            Debug.LogError("Preencha 'Monstro Prefab' no asset do Evento (arraste um prefab do Project).");
            return;
        }

        var combateRootGO = combateUI.painelCombate != null ? combateUI.painelCombate : combateUI.gameObject;

        bool combateNoMesmoPainel =
            painelEvento != null &&
            combateRootGO != null &&
            (combateRootGO.transform == painelEvento.transform ||   // <- trata "self"
             combateRootGO.transform.IsChildOf(painelEvento.transform));

        Debug.Log($"[EventoUI] combateRootGO={{combateRootGO?.name}}, painelEvento={{painelEvento?.name}}, combateNoMesmoPainel={{combateNoMesmoPainel}}");

        if (combateNoMesmoPainel)
        {
            if (combateRootGO.transform != painelEvento.transform)
            {
                var topCombateChild = GetTopLevelChildUnder(painelEvento.transform, combateRootGO.transform);
                for (int i = 0; i < painelEvento.transform.childCount; i++)
                {
                    var child = painelEvento.transform.GetChild(i);
                    bool ehCombateTopo = (child == topCombateChild);
                    child.gameObject.SetActive(ehCombateTopo);
                }
                Debug.Log($"[EventoUI] Ocultou filhos do painelEvento exceto '{{topCombateChild?.name}}'.");
            }
            else
            {
                Debug.Log("[EventoUI] combateRootGO é o próprio painelEvento, não oculta filhos.");
            }
        }
        else
        {
            if (painelEvento != null) painelEvento.SetActive(false);
            Debug.Log("[EventoUI] painelEvento desativado (combate fora do painel).");
        }

            // Sempre preenche o monstro no respectivo slot
            Debug.Log($"[EventoUI] Chamando EventoCombateUI.AbrirMonstro para '{monstroGO.name}'");
            combateUI.AbrirMonstro(monstroGO);

            // Se houver herói selecionado, preenche também o slot do herói
            if (heroiSelecionado != null)
            {
                Debug.Log($"[EventoUI] Chamando EventoCombateUI.AbrirHeroi para '{heroiSelecionado.name}'");
                combateUI.AbrirHeroi(heroiSelecionado.gameObject);
            }

            // Informa a opção atual ao combate (para saber drops)
            if (opcaoSelecionada != null)
            {
                combateUI.DefinirOpcao(opcaoSelecionada);
            }
    }

    public void AbrirPassivo()
    {
        Debug.Log("[EventoUI] AbrirPassivo chamado pelo botão!");
        if (passivoUI == null) passivoUI = UnityEngine.Object.FindFirstObjectByType<PassivoUI>(FindObjectsInactive.Include);
        if (passivoUI == null) { Debug.LogError("PassivoUI não encontrado na cena."); return; }
        if (eventoAtual == null) { Debug.LogError("Nenhum evento atual."); return; }
        if (opcaoSelecionada == null || opcaoSelecionada.tipo != TipoEvento.Passivo)
        {
            Debug.LogWarning("[EventoUI] AbrirPassivo chamado sem opção passiva selecionada.");
            return;
        }

        var passivoRootGO = passivoUI.painelPassivo != null ? passivoUI.painelPassivo : passivoUI.gameObject;

        bool passivoNoMesmoPainel =
            painelEvento != null && passivoRootGO != null &&
            (passivoRootGO.transform == painelEvento.transform || passivoRootGO.transform.IsChildOf(painelEvento.transform));

        if (passivoNoMesmoPainel)
        {
            if (passivoRootGO.transform != painelEvento.transform)
            {
                var topPassivoChild = GetTopLevelChildUnder(painelEvento.transform, passivoRootGO.transform);
                for (int i = 0; i < painelEvento.transform.childCount; i++)
                {
                    var child = painelEvento.transform.GetChild(i);
                    bool ehPassivoTopo = (child == topPassivoChild);
                    child.gameObject.SetActive(ehPassivoTopo);
                }
            }
        }
        else
        {
            if (painelEvento != null) painelEvento.SetActive(false);
        }

        passivoUI.AbrirOpcaoPassiva(eventoAtual, opcaoSelecionada);
    }

    // Novo fluxo: aceite na fase Intro
    public void OnClickAceite()
    {
        // Fluxo simplificado: a partir da IntroFiltrada vai direto para Opcoes.
        if (faseAtual == FaseVisualEvento.IntroFiltrada)
        {
            if (heroiSelecionado == null)
            {
                Debug.LogWarning("[EventoUI] Necessário selecionar um herói antes de aceitar (IntroFiltrada).");
                return;
            }
            Debug.Log($"[EventoUI] Aceite (ID={eventoInstanceId ?? "<null>"}) na IntroFiltrada. Herói='{heroiSelecionado.name}'. Fechando painel e programando reabrir em {delayReabrirSeg:0.##}s.");
            if (botaoAceite != null)
            {
                botaoAceite.interactable = false;
                botaoAceite.gameObject.SetActive(false);
            }
            // Snapshot dos dados atuais para este evento (evita sobrescrita se outro evento abrir antes do delay)
            var snapPrefab = ultimoRespawnPrefab;
            var snapParent = ultimoRespawnParent;
            var snapPos = ultimoRespawnPos;
            var snapRot = ultimoRespawnRot;
            // Novos snapshots específicos do evento e herói para garantir independência entre múltiplos eventos aceitos em sequência
            var snapEvento = eventoAtual;      // armazena o Evento desta instância
            var snapHeroi = heroiSelecionado;  // armazena o herói selecionado no momento do aceite
            // Desativa/destroi ícone original agora (caso ainda esteja visível porque esconderAoAbrir=false)
            if (origemIconeAtual != null && origemIconeAtual.activeSelf)
            {
                origemIconeAtual.SetActive(false);
                Debug.Log($"[EventoUI] Ícone original desativado no aceite (ID={eventoInstanceId}).");
            }
            // Fecha a primeira tela do evento
            Fechar();
            // Agenda usando TODOS os snapshots (inclui evento/herói para não serem trocados por outro evento aberto antes do delay)
            StartCoroutine(ReabrirAposDelaySnapshot(eventoInstanceId, snapPrefab, snapParent, snapPos, snapRot, snapEvento, snapHeroi));
            return;
        }

        // Se já estiver em Opcoes não há mais ação do botão
        if (faseAtual == FaseVisualEvento.Opcoes)
        {
            Debug.Log("[EventoUI] Botão aceite clicado em fase Opcoes (sem efeito).");
        }
    }

    // Removidos: campos e lógica de spawn do ícone de reabrir

    // Permite que o spawner/ícone informe de onde veio o primeiro ícone
    public void DefinirUltimaOrigemIcone(Transform origem, GameObject prefabMundo, Evento evento)
    {
        // Tenta obter dados de respawn anexados ao ícone original
        ultimoRespawnPrefab = null;
        ultimoRespawnParent = null;
        ultimoRespawnPos = Vector3.zero;
        ultimoRespawnRot = Quaternion.identity;

        if (origem != null)
        {
            var data = origem.GetComponent<EventoIconeRespawnData>();
            if (data != null && data.respawnPrefab != null)
            {
                ultimoRespawnPrefab = data.respawnPrefab;
                ultimoRespawnParent = data.parent;
                ultimoRespawnPos = data.worldPosition;
                ultimoRespawnRot = data.worldRotation;
                Debug.Log($"[EventoUI] Origem registrada via EventoIconeRespawnData. Prefab='{ultimoRespawnPrefab.name}', parent='{ultimoRespawnParent?.name}', pos={ultimoRespawnPos}.");
                return;
            }
        }

        // Fallback: usa o próprio ícone/origem quando não há EventoIconeRespawnData
        if (prefabMundo != null)
        {
            ultimoRespawnPrefab = prefabMundo;
            ultimoRespawnParent = origem != null ? origem.parent : null;
            ultimoRespawnPos = origem != null ? origem.position : Vector3.zero;
            ultimoRespawnRot = origem != null ? origem.rotation : Quaternion.identity;
            Debug.Log($"[EventoUI] Origem registrada (fallback). Prefab='{ultimoRespawnPrefab.name}', parent='{ultimoRespawnParent?.name}', pos={ultimoRespawnPos}.");
        }
        else
        {
            Debug.LogWarning("[EventoUI] DefinirUltimaOrigemIcone: sem dados de respawn e sem prefab de fallback.");
        }
    }

    // Compat: versão antiga que usa estado atual (mantida caso algo externo chame)
    IEnumerator ReabrirAposDelay()
    {
        // Wrapper legado: captura snapshots atuais (pode estar incorreto se outro evento abriu depois, mas mantém compat). Preferir fluxo novo.
        yield return ReabrirAposDelaySnapshot(eventoInstanceId ?? "legacy", ultimoRespawnPrefab, ultimoRespawnParent, ultimoRespawnPos, ultimoRespawnRot, eventoAtual, heroiSelecionado);
    }

    // Nova versão parametrizada: inclui evento/herói para evitar interferência entre múltiplos eventos simultâneos (race conditions)
    IEnumerator ReabrirAposDelaySnapshot(string instanceId, GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, Evento eventoSnap, HeroiSelecionavel heroiSnap)
    {
        if (delayReabrirSeg > 0f)
            yield return new WaitForSeconds(delayReabrirSeg);

        if (prefab == null)
        {
            Debug.LogWarning($"[EventoUI] ReabrirAposDelaySnapshot(ID={instanceId}) prefab nulo, nenhum ícone para reabrir.");
            yield break;
        }

        var go = Instantiate(prefab, pos, rot, parent);
        Debug.Log($"[EventoUI] Ícone de reabrir instanciado (ID={instanceId}): '{go.name}' em {pos} (parent='{parent?.name ?? "<none>"}') evento='{eventoSnap?.name}' heroi='{heroiSnap?.name}'");

        if (go.GetComponent<Collider2D>() == null)
            go.AddComponent<BoxCollider2D>();
        // Se o prefab tiver o componente de clique padrão (EventoIconeClick), removemos para evitar conflito
        // com o comportamento específico de reabrir (EventoReabrirIcon). Isso evita erro de 'evento não definido'.
        var clickPadrao = go.GetComponent<EventoIconeClick>();
        if (clickPadrao != null)
        {
            clickPadrao.enabled = false;
            Destroy(clickPadrao);
        }
        var reabrirCmp = go.GetComponent<EventoReabrirIcon>();
        if (reabrirCmp == null)
            reabrirCmp = go.AddComponent<EventoReabrirIcon>();
        if (reabrirCmp != null)
            reabrirCmp.Configurar(this, instanceId, eventoSnap, heroiSnap);
    }

    public void AbrirOpcoesFromIcon(GameObject icon)
    {
        Debug.Log($"[EventoUI] AbrirOpcoesFromIcon chamado. eventoAtual='{eventoAtual?.name}', heroi='{heroiSelecionado?.name}'");
        // Compatibilidade: simplesmente abre a fase de opções (sem standby/respawn)
        faseAtual = FaseVisualEvento.Opcoes;
        if (icon != null) Destroy(icon);
        if (painelEvento != null)
        {
            if (painelEvento == this.gameObject && !gameObject.activeSelf)
                gameObject.SetActive(true);
            SetPainelEventoVisivel(true);
            RestaurarLayoutEvento();
        }
        if (botaoAceite != null) botaoAceite.gameObject.SetActive(false);
        // Atualiza título e descrição com o evento atual (necessário ao reabrir direto em Opções para outro evento)
        if (nomeEventoText != null) nomeEventoText.text = eventoAtual != null ? eventoAtual.nomeEvento : "";
        if (descricaoEventoText != null) descricaoEventoText.text = eventoAtual != null ? eventoAtual.descricaoEvento : "";
        SelecionarHeroi(heroiSelecionado);
        GerarBotoesDeOpcoes();
        DebugChildrenEstado();
    }

    // Abre diretamente a fase de opções a partir de um ícone respawnado, usando os dados vinculados àquele ícone
    public void AbrirOpcoesFromRespawn(string instanceId, Evento evento, HeroiSelecionavel heroi, GameObject icon)
    {
        this.eventoInstanceId = instanceId;
        this.eventoAtual = evento;
        this.heroiSelecionado = heroi;
        AbrirOpcoesFromIcon(icon);
    }

    // Deve ser chamado pelo fluxo de combate/passivo quando o evento terminar.
    // Se usar multi-instância futuramente, será movido para painel individual.
    public void FinalizarEventoAtual()
    {
        if (eventoAtual == null)
        {
            Debug.LogWarning("[EventoUI] FinalizarEventoAtual chamado sem eventoAtual.");
            return;
        }
        var local = eventoAtual.local;
        Debug.Log($"[EventoUI] FinalizarEventoAtual: evento='{eventoAtual.name}', local={local}");
        OnEventoResolvido?.Invoke(local, eventoAtual);
        // Fecha painel atual (mantém singleton pronto para outro)
        Fechar();
        // Limpa estado interno para evitar interferência em novos eventos
        eventoAtual = null;
        opcaoSelecionada = null;
        heroiSelecionado = null;
    }

    // Oculta tudo exceto título, descrição, slot herói e botão aceite (para fase IntroFiltrada)
    void FiltrarLayoutIntro()
    {
        if (painelEvento == null) return;
        var permitidos = new HashSet<Transform>();
        if (nomeEventoText != null) permitidos.Add(nomeEventoText.transform);
        if (descricaoEventoText != null) permitidos.Add(descricaoEventoText.transform);
        if (slotHeroiImage != null) permitidos.Add(slotHeroiImage.transform);
        if (botaoAceite != null) permitidos.Add(botaoAceite.transform);

        // Ativa somente os filhos top-level que contêm pelo menos um dos transform permitidos
        for (int i = 0; i < painelEvento.transform.childCount; i++)
        {
            var child = painelEvento.transform.GetChild(i);
            bool manter = false;
            foreach (var tPerm in permitidos)
            {
                if (tPerm != null && (tPerm == child || tPerm.IsChildOf(child))) { manter = true; break; }
            }
            child.gameObject.SetActive(manter);
        }
    }

        // Reativa todos os filhos do painel de evento exceto o root de combate (se existir)
        void RestaurarLayoutEvento()
        {
            if (painelEvento == null) return;
            Transform combateRoot = null;
            if (combateUI != null)
            {
                combateRoot = (combateUI.painelCombate != null) ? combateUI.painelCombate.transform : combateUI.transform;
            }
            for (int i = 0; i < painelEvento.transform.childCount; i++)
            {
                var child = painelEvento.transform.GetChild(i);
                bool ehCombateRoot = (combateRoot != null && child == combateRoot);
                // Evento aberto: queremos mostrar tudo MENOS o layout de combate
                child.gameObject.SetActive(!ehCombateRoot);
            }
        }

    // Retorna o filho direto de 'parent' que contém 'descendant'
    static Transform GetTopLevelChildUnder(Transform parent, Transform descendant)
    {
        if (parent == null || descendant == null) return null;
        var t = descendant;
        while (t != null && t.parent != parent) t = t.parent;
        return (t != null && t.parent == parent) ? t : null;
    }

    void DebugChildrenEstado()
    {
        if (painelEvento == null) { Debug.Log("[EventoUI] painelEvento nulo no DebugChildrenEstado"); return; }
        Debug.Log("[EventoUI] --- Estado filhos do painelEvento ao Abrir ---");
        for (int i = 0; i < painelEvento.transform.childCount; i++)
        {
            var c = painelEvento.transform.GetChild(i);
            var rt = c as RectTransform;
            string size = rt != null ? $"(w={rt.rect.width},h={rt.rect.height},pos={rt.anchoredPosition})" : "";
            Debug.Log($"[EventoUI] Child {i}: {c.name} ativo={c.gameObject.activeSelf} {size}");
        }
        if (botaoAceite != null)
        {
            var rtB = botaoAceite.GetComponent<RectTransform>();
            string sizeB = rtB != null ? $"(w={rtB.rect.width},h={rtB.rect.height},pos={rtB.anchoredPosition})" : "";
            Debug.Log($"[EventoUI] BotãoAceite ativo={botaoAceite.gameObject.activeSelf} interativo={botaoAceite.interactable} {sizeB}");
        }
    }

    // Oculta/mostra o conteúdo do painel sem desativar o GameObject do EventoUI quando painelEvento == this.gameObject
    void SetPainelEventoVisivel(bool visivel)
    {
        if (painelEvento == null) return;
        if (painelEvento == this.gameObject)
        {
            // Não desativa o root; alterna só os filhos
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.gameObject.SetActive(visivel);
            }
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visivel ? 1f : 0f;
            cg.interactable = visivel;
            cg.blocksRaycasts = visivel;
            Debug.Log($"[EventoUI] SetPainelEventoVisivel: root mantido ativo, filhos={(visivel ? "ON" : "OFF")}.");
        }
        else
        {
            painelEvento.SetActive(visivel);
            Debug.Log($"[EventoUI] SetPainelEventoVisivel: painelEvento.SetActive({visivel}).");
        }
    }
}
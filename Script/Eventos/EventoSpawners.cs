using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro; // Necessário para o texto da UI

public class EventoSpawner : MonoBehaviour
{
    public static EventoSpawner Instance; 

    [Header("Configuração Visual")]
    public GameObject iconeEventoPrefab;
    public RectTransform uiContainer;
    public EventoUI eventoUIManager;
    
    [Header("UI do Calendário")]
    [Tooltip("Arraste aqui o TextMeshProUGUI que ficará no topo da tela.")]
    public TextMeshProUGUI txtCalendario;

    [Header("Carregamento Automático (Pastas)")]
    [Tooltip("Digite o caminho da pasta DENTRO de uma pasta 'Resources'. Ex: 'Eventos/Floresta'")]
    public List<string> pastasResources;

    [Header("Banco de Dados de Eventos")]
    [Tooltip("Essa lista será preenchida automaticamente com os itens das pastas acima + itens manuais.")]
    public List<Evento> eventosPossiveis = new List<Evento>(); 

    [Header("Ciclo de Tempo")]
    [Tooltip("Quantos eventos precisam ser finalizados para passar 1 semana.")]
    public int eventosPorSemana = 5; 
    
    // Variáveis de Estado (Read-only no inspector para debug)
    [Header("Estado Atual (Debug)")]
    public int eventosFinalizados = 0;
    public int semanaAtual = 1;
    public int mesAtual = 1;

    [Header("Opções")]
    public bool spawnarAoIniciar = true;

    // --- NOVO: Histórico de Conclusão ---
    // Armazena os Eventos que foram concluídos com SUCESSO
    private HashSet<Evento> historicoConcluidos = new HashSet<Evento>();
    // ------------------------------------

    // Lista para rastrear os botões spawnados na cena
    private List<GameObject> botoesAtivos = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Carregar eventos das pastas
        CarregarEventosDasPastas();
    }

    void CarregarEventosDasPastas()
    {
        if (pastasResources == null || pastasResources.Count == 0) return;

        int totalCarregado = 0;

        foreach (string caminho in pastasResources)
        {
            Evento[] eventosDaPasta = Resources.LoadAll<Evento>(caminho);

            if (eventosDaPasta != null && eventosDaPasta.Length > 0)
            {
                eventosPossiveis.AddRange(eventosDaPasta);
                totalCarregado += eventosDaPasta.Length;
                Debug.Log($"[Spawner] Carregados {eventosDaPasta.Length} eventos da pasta 'Resources/{caminho}'");
            }
            else
            {
                Debug.LogWarning($"[Spawner] Nenhum evento encontrado no caminho 'Resources/{caminho}'.");
            }
        }
        
        Debug.Log($"[Spawner] Total de eventos prontos: {eventosPossiveis.Count} (Sendo {totalCarregado} via pastas).");
    }

    void Start()
    {
        if (semanaAtual < 1) semanaAtual = 1;
        if (mesAtual < 1) mesAtual = 1;
        eventosFinalizados = 0; 
        
        AtualizarTextoCalendario();

        if (spawnarAoIniciar)
        {
            InicializarTodosPontos();
        }
    }

    void InicializarTodosPontos()
    {
        var pontos = FindObjectsByType<EventoLocalPoint>(FindObjectsSortMode.None);
        Debug.Log($"[Spawner] Inicializando {pontos.Length} pontos de evento na cena.");

        foreach (var ponto in pontos)
        {
            if (ponto.habilitado)
            {
                CriarBotao(ponto);
            }
        }
    }

    public bool PodeSpawnarNovo()
    {
        return true; 
    }

    // --- MÉTODO ATUALIZADO: Recebe o evento e se foi sucesso ---
    public void RegistrarEventoFinalizado(Evento eventoConcluido, bool sucesso)
    {
        // Se foi sucesso, adiciona ao histórico para desbloquear futuras aventuras
        if (sucesso && eventoConcluido != null)
        {
            if (!historicoConcluidos.Contains(eventoConcluido))
            {
                historicoConcluidos.Add(eventoConcluido);
                Debug.Log($"[Spawner] Evento '{eventoConcluido.nomeEvento}' registrado como SUCESSO no histórico.");
            }
        }
        else
        {
            Debug.Log($"[Spawner] Evento '{eventoConcluido?.nomeEvento}' finalizado sem sucesso (ou fugiu). Não conta para histórico de aventuras.");
        }

        eventosFinalizados++;
        
        if (eventosFinalizados >= eventosPorSemana)
        {
            PassarSemana();
        }
        else
        {
            AtualizarTextoCalendario();
        }

        Debug.Log($"[Spawner] Progresso Semanal: {eventosFinalizados}/{eventosPorSemana}");
    }

    void PassarSemana()
    {
        eventosFinalizados = 0; // Reseta contagem para a nova semana
        semanaAtual++;

        // Regra: A cada 4 semanas, vira o mês
        if (semanaAtual > 4)
        {
            semanaAtual = 1;
            mesAtual++;
            Debug.Log($"[Spawner] Mês virou! Bem-vindo ao Mês {mesAtual}");
        }
        else
        {
            Debug.Log($"[Spawner] Semana virou! Estamos na Semana {semanaAtual}");
        }

        LimparEventosAtuais();
        AtualizarTextoCalendario();
        
        // Respowna novos eventos baseados na nova semana
        InicializarTodosPontos();
    }

    void LimparEventosAtuais()
    {
        foreach (GameObject btn in botoesAtivos)
        {
            if (btn != null) Destroy(btn);
        }
        botoesAtivos.Clear();
        Debug.Log("[Spawner] Mapa limpo para a nova semana.");
    }

    void AtualizarTextoCalendario()
    {
        if (txtCalendario != null)
        {
            txtCalendario.text = $"Mês {mesAtual} | Semana {semanaAtual}\nProgresso: {eventosFinalizados}/{eventosPorSemana}";
        }
    }

    // --- LÓGICA PRINCIPAL DE FILTRO E PRIORIDADE ---
    public Evento ObterEventoAleatorio(EventoLocal local)
    {
        if (eventosPossiveis == null || eventosPossiveis.Count == 0) return null;

        // 1. Filtra eventos válidos para o LOCAL e SEMANA atuais
        var validosGeral = eventosPossiveis.Where(e => 
            e.local == local && 
            e.semanaMin <= semanaAtual && 
            e.semanaMax >= semanaAtual
        ).ToList();

        if (validosGeral.Count == 0) return null;

        // 2. Separa em listas de candidatos
        List<Evento> aventurasCandidatas = new List<Evento>();
        List<Evento> normaisCandidatos = new List<Evento>();

        foreach (var ev in validosGeral)
        {
            // Se já foi concluído e é único (Assumindo que Aventuras não repetem depois de feitas), ignoramos.
            // (Para eventos normais, se quiser que repitam, remova a checagem do histórico para eles)
            if (historicoConcluidos.Contains(ev) && ev.categoria == CategoriaEvento.Aventura) 
                continue;

            if (ev.categoria == CategoriaEvento.Aventura)
            {
                // Regra da Aventura:
                // Só pode ocorrer se não tiver antecessor OU se o antecessor estiver no histórico.
                if (ev.antecessor == null || historicoConcluidos.Contains(ev.antecessor))
                {
                    aventurasCandidatas.Add(ev);
                }
            }
            else
            {
                // Eventos normais entram direto
                normaisCandidatos.Add(ev);
            }
        }

        // 3. Prioridade: Se houver Aventura disponível, ela tem prioridade total
        if (aventurasCandidatas.Count > 0)
        {
            Debug.Log($"[Spawner] Priorizando Aventura no local {local}. Opções: {aventurasCandidatas.Count}");
            return aventurasCandidatas[Random.Range(0, aventurasCandidatas.Count)];
        }

        // 4. Caso contrário, sorteia um normal
        if (normaisCandidatos.Count > 0)
        {
            return normaisCandidatos[Random.Range(0, normaisCandidatos.Count)];
        }

        return null;
    }

    public void SpawnarEventosNoLocal(EventoLocal localAlvo)
    {
        var pontos = FindObjectsByType<EventoLocalPoint>(FindObjectsSortMode.None);
        foreach (var ponto in pontos)
        {
            if (ponto.habilitado && ponto.local == localAlvo)
            {
                CriarBotao(ponto);
            }
        }
    }

    void CriarBotao(EventoLocalPoint ponto)
    {
        if (iconeEventoPrefab == null || uiContainer == null) return;

        Evento eventoSorteado = ObterEventoAleatorio(ponto.local);
        
        if (eventoSorteado == null) return;

        GameObject btnObj = Instantiate(iconeEventoPrefab, uiContainer);

        botoesAtivos.Add(btnObj); 

        // Z-ORDER: joga para o fundo da hierarquia para ser renderizado primeiro (ou usar SortingGroup)
        btnObj.transform.SetAsFirstSibling(); 
        
        UIFollowWorldObject seguidor = btnObj.GetComponent<UIFollowWorldObject>();
        if (seguidor == null) seguidor = btnObj.AddComponent<UIFollowWorldObject>();
        seguidor.SetTarget(ponto.transform, ponto.spawnOffset);

        BotaoEventoMapa scriptBotao = btnObj.GetComponent<BotaoEventoMapa>();
        if (scriptBotao != null)
        {
            if (eventoUIManager == null) 
                eventoUIManager = FindFirstObjectByType<EventoUI>(FindObjectsInactive.Include);

            scriptBotao.Configurar(eventoSorteado, eventoUIManager, ponto.local);
        }
    }
}
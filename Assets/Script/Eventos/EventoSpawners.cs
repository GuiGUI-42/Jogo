using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Adicionado para usar Where e ToList

public class EventoSpawner : MonoBehaviour
{
    public static EventoSpawner Instance; // Singleton para acesso global

    [Header("Configuração")]
    public GameObject iconeEventoPrefab;
    public RectTransform uiContainer;
    public EventoUI eventoUIManager;

    [Header("Banco de Eventos")]
    public List<Evento> eventosPossiveis; 

    [Header("Ciclo de Jogo")]
    public int limiteEventosDia = 5; // Limite de eventos por ciclo
    public int eventosFinalizados = 0; // Contador atual

    [Header("Teste Inicial")]
    public bool spawnarAoIniciar = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        eventosFinalizados = 0; // Reseta contagem ao iniciar
        if (spawnarAoIniciar)
        {
            SpawnarEventosNoLocal(EventoLocal.Cidade);
        }
    }

    // Chamado pelos botões para saber se ainda podem aparecer
    public bool PodeSpawnarNovo()
    {
        return eventosFinalizados < limiteEventosDia;
    }

    // Chamado pela UI (EventoUI) quando um evento termina totalmente
    public void RegistrarEventoFinalizado()
    {
        eventosFinalizados++;
        Debug.Log($"[Spawner] Evento finalizado! Progresso: {eventosFinalizados}/{limiteEventosDia}");

        if (eventosFinalizados >= limiteEventosDia)
        {
            Debug.Log("=== DIA ENCERRADO! ===");
            // Aqui você pode colocar lógica de fim de dia
        }
    }

    // --- NOVO MÉTODO PARA SORTEAR EVENTO ALEATÓRIO POR LOCAL ---
    public Evento ObterEventoAleatorio(EventoLocal local)
    {
        if (eventosPossiveis == null || eventosPossiveis.Count == 0) return null;

        // Filtra eventos que pertencem ao local solicitado
        var eventosDoLocal = eventosPossiveis.Where(e => e.local == local).ToList();

        if (eventosDoLocal.Count == 0)
        {
            Debug.LogWarning($"[Spawner] Nenhum evento encontrado para o local: {local}");
            return null;
        }

        // Retorna um aleatório da lista filtrada (chances iguais)
        return eventosDoLocal[Random.Range(0, eventosDoLocal.Count)];
    }

    public void SpawnarEventosNoLocal(EventoLocal localAlvo)
    {
        var pontos = FindObjectsByType<EventoLocalPoint>(FindObjectsSortMode.None);
        Debug.Log($"[Spawner] Criando botões para {localAlvo}. Pontos encontrados: {pontos.Length}");

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

        // Sorteia o primeiro evento
        Evento eventoSorteado = ObterEventoAleatorio(ponto.local);
        if (eventoSorteado == null) return;

        GameObject btnObj = Instantiate(iconeEventoPrefab, uiContainer);

        UIFollowWorldObject seguidor = btnObj.GetComponent<UIFollowWorldObject>();
        if (seguidor == null) seguidor = btnObj.AddComponent<UIFollowWorldObject>();
        seguidor.SetTarget(ponto.transform, ponto.spawnOffset);

        BotaoEventoMapa scriptBotao = btnObj.GetComponent<BotaoEventoMapa>();
        if (scriptBotao != null)
        {
            if (eventoUIManager == null) 
                eventoUIManager = FindFirstObjectByType<EventoUI>();

            // Passamos também o ponto.local para o botão saber qual seu tipo de terreno
            scriptBotao.Configurar(eventoSorteado, eventoUIManager, ponto.local);
        }
    }
}
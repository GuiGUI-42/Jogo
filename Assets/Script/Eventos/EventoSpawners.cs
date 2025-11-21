using UnityEngine;
using System.Collections.Generic;

public class EventoSpawner : MonoBehaviour
{
    [Header("Configuração")]
    public GameObject iconeEventoPrefab; // Seu prefab "BotãoAceite"
    public RectTransform uiContainer;    // Seu "Canvas_eventos"
    public EventoUI eventoUIManager;     // Referência à Janela de Evento na cena

    [Header("Banco de Eventos")]
    // Arraste aqui seus assets (Incendio, Roubo, Goblin...)
    public List<Evento> eventosPossiveis; 

    [Header("Teste Inicial")]
    public bool spawnarAoIniciar = true;

    void Start()
    {
        if (spawnarAoIniciar)
        {
            SpawnarEventosNoLocal(EventoLocal.Cidade);
        }
    }

    public void SpawnarEventosNoLocal(EventoLocal localAlvo)
{
    // Procura todos os pontos na cena
    var pontos = FindObjectsByType<EventoLocalPoint>(FindObjectsSortMode.None);

    // LOG 1: Avisa quantos pontos achou no total
    Debug.Log($"[Spawner] Buscando locais... Encontrei {pontos.Length} objetos com 'EventoLocalPoint' na cena.");

    if (pontos.Length == 0)
    {
        Debug.LogError("[Spawner] ERRO: Nenhum 'EventoLocalPoint' encontrado na cena! Você adicionou o script na Cidade?");
        return;
    }

    foreach (var ponto in pontos)
    {
        // LOG 2: Mostra o que está verificando
        Debug.Log($"[Spawner] Verificando objeto: {ponto.name} | Local configurado: {ponto.local} | Habilitado: {ponto.habilitado}");

        if (ponto.habilitado && ponto.local == localAlvo)
        {
            Debug.Log($"[Spawner] SUCESSO! Criando botão em: {ponto.name}");
            CriarBotao(ponto);
        }
    }
}

    void CriarBotao(EventoLocalPoint ponto)
    {
        // LOG DE ENTRADA
        Debug.Log($"[Diagnostico] Tentando criar botão para: {ponto.name}...");

        // 1. VERIFICAÇÕES INDIVIDUAIS (Para saber qual está falhando)
        if (iconeEventoPrefab == null)
        {
            Debug.LogError("[ERRO CRÍTICO] O campo 'Icone Evento Prefab' está VAZIO no Inspector!");
            return;
        }
        if (uiContainer == null)
        {
            Debug.LogError("[ERRO CRÍTICO] O campo 'Ui Container' está VAZIO no Inspector!");
            return;
        }
        if (eventosPossiveis == null || eventosPossiveis.Count == 0)
        {
            Debug.LogError("[ERRO CRÍTICO] A lista 'Eventos Possiveis' está VAZIA! Adicione os assets de evento.");
            return;
        }

        // 2. TENTATIVA DE INSTANCIAÇÃO
        Debug.Log("[Diagnostico] Tudo ok. Instanciando agora...");
        
        Evento eventoSorteado = eventosPossiveis[Random.Range(0, eventosPossiveis.Count)];
        GameObject btnObj = Instantiate(iconeEventoPrefab, uiContainer);

        if (btnObj == null)
        {
            Debug.LogError("[ERRO ESTRANHO] O Instantiate retornou nulo! O Prefab pode estar corrompido?");
            return;
        }
        
        Debug.Log($"[Diagnostico] Objeto instanciado com sucesso: {btnObj.name}");

        // 3. POSICIONAMENTO
        UIFollowWorldObject seguidor = btnObj.GetComponent<UIFollowWorldObject>();
        if (seguidor == null) seguidor = btnObj.AddComponent<UIFollowWorldObject>();
        seguidor.SetTarget(ponto.transform, ponto.spawnOffset);

        // 4. CONFIGURAÇÃO
        BotaoEventoMapa scriptBotao = btnObj.GetComponent<BotaoEventoMapa>();
        if (scriptBotao != null)
        {
            if (eventoUIManager == null) 
                eventoUIManager = FindFirstObjectByType<EventoUI>();

            scriptBotao.Configurar(eventoSorteado, eventoUIManager);
            Debug.Log("[Diagnostico] Botão configurado completamente!");
        }
        else
        {
            Debug.LogError($"[ERRO] O Prefab '{btnObj.name}' não tem o script 'BotaoEventoMapa'!");
        }
    }
}
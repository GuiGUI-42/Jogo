using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class EventoSpawner : MonoBehaviour
{
    [Header("Eventos Globais")]
    [Tooltip("Lista completa de eventos disponíveis (o spawner filtrará por local em cada ponto).")]
    public Evento[] eventosDisponiveis;

    [Header("Ícones Globais")]
    [Tooltip("Prefabs possíveis de ícone; o spawner escolherá um compatível com o local do ponto.")]
    public GameObject[] iconePrefabs;
    [Tooltip("Prefab usado para respawn futuro (aplicado aos ícones gerados).")]
    public GameObject iconeRespawnPrefab;

    [Header("Spawn Geral")]
    [Tooltip("Aguardar esse tempo antes de processar todos os pontos de spawn.")]
    public float delayInicial = 2f;
    [Tooltip("Se verdadeiro, não volta a spawnar ícones em pontos que já possuem um filho com EventoIconeClick ativo.")]
    public bool evitarDuplicarNoMesmoPonto = true;
    [Tooltip("Log detalhado para depuração.")]
    public bool logDetalhado = true;

    [Header("Sequencial por Localidade")]
    [Tooltip("Se verdadeiro, spawna um ícone por localidade em sequência ao iniciar.")]
    public bool spawnSequencialPorLocalidade = true;
    [Tooltip("Intervalo entre spawns de localidades diferentes (segundos).")]
    public float delayEntreLocalidades = 5f;
    [Tooltip("Delay para respawn após resolver evento daquele local.")]
    public float delayRespawnPorLocal = 2f;

    // Controle de bloqueio por local (um evento ativo/ícone pendente por local)
    readonly HashSet<EventoLocal> locaisBloqueados = new HashSet<EventoLocal>();
    // Ícone atualmente spawnado por local (para verificação extra / limpeza futura)
    readonly Dictionary<EventoLocal, GameObject> iconeAtualPorLocal = new Dictionary<EventoLocal, GameObject>();

    void OnEnable()
    {
        EventoUI.OnEventoResolvido += HandleEventoResolvido;
    }

    void OnDisable()
    {
        EventoUI.OnEventoResolvido -= HandleEventoResolvido;
    }

    void Start()
    {
        if (spawnSequencialPorLocalidade)
            StartCoroutine(SpawnSequencialInicial());
        else
            StartCoroutine(SpawnTodosAposDelay());
    }

    void HandleEventoResolvido(EventoLocal local, Evento evento)
    {
        if (locaisBloqueados.Contains(local))
        {
            if (logDetalhado)
                Debug.Log($"[EventoSpawner] Evento resolvido para local {local}. Liberando para próximo spawn.");
            locaisBloqueados.Remove(local);
        }
        if (delayRespawnPorLocal > 0f)
            StartCoroutine(SpawnLocalAposDelay(local, delayRespawnPorLocal));
        else
            SpawnParaLocal(local);
    }

    IEnumerator SpawnLocalAposDelay(EventoLocal local, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnParaLocal(local);
    }

    IEnumerator SpawnSequencialInicial()
    {
        if (delayInicial > 0f)
            yield return new WaitForSeconds(delayInicial);

        // Agrupa pontos por local
        var pontos = UnityEngine.Object.FindObjectsByType<EventoLocalPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (pontos == null || pontos.Length == 0)
        {
            Debug.LogWarning("[EventoSpawner] Nenhum EventoLocalPoint na cena para sequência inicial.");
            yield break;
        }
        var grupos = pontos.Where(p => p != null && p.habilitado)
                            .GroupBy(p => p.local)
                            .ToList();
        foreach (var grupo in grupos)
        {
            var local = grupo.Key;
            if (logDetalhado)
                Debug.Log($"[EventoSpawner] Sequencial inicial: tentando spawn para local={local} (pontos={grupo.Count()})");
            if (!locaisBloqueados.Contains(local))
                SpawnParaLocal(local);
            if (delayEntreLocalidades > 0f)
                yield return new WaitForSeconds(delayEntreLocalidades);
        }
    }

    IEnumerator SpawnTodosAposDelay()
    {
        if (delayInicial > 0f)
            yield return new WaitForSeconds(delayInicial);

        // Busca pontos de spawn na cena
        var pontos = UnityEngine.Object.FindObjectsByType<EventoLocalPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (logDetalhado)
        {
            Debug.Log(pontos != null ? $"[EventoSpawner] Pontos encontrados: {pontos.Length}" : "[EventoSpawner] Pontos array nulo");
            if (pontos != null)
            {
                foreach (var p in pontos)
                {
                    if (p != null)
                        Debug.Log($"[EventoSpawner] Ponto: name='{p.name}', ativo={p.gameObject.activeSelf}, habilitado={p.habilitado}, local={p.local}");
                }
            }
        }
        if (pontos == null || pontos.Length == 0)
        {
            Debug.LogWarning("EventoSpawner: nenhum EventoLocalPoint encontrado na cena.");
            yield break;
        }

        foreach (var ponto in pontos)
        {
            if (ponto == null || !ponto.habilitado)
                continue;

            // Bloqueio por local: se já bloqueado, ignora este ponto
            if (locaisBloqueados.Contains(ponto.local))
            {
                if (logDetalhado)
                    Debug.Log($"[EventoSpawner] Local '{ponto.local}' bloqueado; não spawnar outro ícone agora (ponto '{ponto.name}').");
                continue;
            }

            // Verifica duplicação (existe um ícone ativo já filho direto?)
            if (evitarDuplicarNoMesmoPonto)
            {
                bool jaTem = ponto.transform.GetComponentsInChildren<EventoIconeClick>(false)
                    .Any(c => c.gameObject.transform.parent == ponto.transform && c.gameObject.activeSelf);
                if (jaTem)
                {
                    if (logDetalhado)
                        Debug.Log($"[EventoSpawner] Ignorando ponto '{ponto.name}' (já possui ícone ativo).");
                    continue;
                }
            }

            var localAlvo = ponto.local;
            var candidatos = (eventosDisponiveis ?? new Evento[0])
                .Where(e => e != null && e.local == localAlvo)
                .ToArray();
            if (candidatos.Length == 0)
            {
                if (logDetalhado)
                    Debug.LogWarning($"[EventoSpawner] Ponto '{ponto.name}' local={localAlvo} sem eventos compatíveis.");
                continue;
            }
            var eventoAleatorio = candidatos[Random.Range(0, candidatos.Length)];
            if (logDetalhado)
                Debug.Log($"[EventoSpawner] Evento escolhido para ponto '{ponto.name}': '{eventoAleatorio?.name}' (total candidatos={candidatos.Length})");

            GameObject prefabIcone = ponto.prefabIconeOverride;
            if (prefabIcone == null)
            {
                if (iconePrefabs == null || iconePrefabs.Length == 0)
                {
                    Debug.LogError("EventoSpawner: 'iconePrefabs' vazio (e ponto sem override). Arraste os prefabs de ícone.");
                    continue;
                }
                var listaCompatíveis = iconePrefabs
                    .Where(p => p != null)
                    .Select(p => new { prefab = p, clic = p.GetComponent<EventoIconeClick>() })
                    .Where(pc => pc.clic != null)
                    .Where(pc => pc.clic.aceitaQualquerLocal || pc.clic.localIcone == localAlvo)
                    .Select(pc => pc.prefab)
                    .ToList();
                if (listaCompatíveis.Count > 0)
                    prefabIcone = listaCompatíveis[Random.Range(0, listaCompatíveis.Count)];
                else
                {
                    if (logDetalhado)
                        Debug.LogWarning($"[EventoSpawner] Nenhum ícone compatível com local={localAlvo}; usando qualquer prefab.");
                    prefabIcone = iconePrefabs[Random.Range(0, iconePrefabs.Length)];
                }
            }

            if (prefabIcone == null)
            {
                Debug.LogError($"[EventoSpawner] Ponto '{ponto.name}' sem prefab de ícone válido (override ou lista global).");
                continue;
            }

            SpawnIconeEmPonto(ponto, prefabIcone, eventoAleatorio);
        }
    }

    // Permite testar manualmente via botão no Inspector (Context Menu) em Play Mode
    [ContextMenu("Spawn Todos Agora")]
    void SpawnTodosAgoraContextMenu()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnTodosAposDelayImmediate());
    }

    IEnumerator SpawnTodosAposDelayImmediate()
    {
        yield return null; // garante que estamos em play
        var pontos = UnityEngine.Object.FindObjectsByType<EventoLocalPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (pontos == null || pontos.Length == 0)
        {
            Debug.LogWarning("[EventoSpawner] (ContextMenu) Nenhum ponto para spawn.");
            yield break;
        }
        foreach (var p in pontos)
        {
            if (p != null && p.habilitado)
            {
                // Reuso da lógica principal chamando método auxiliar
                SpawnParaPonto(p);
            }
        }
    }

    [ContextMenu("Spawn Ponto Selecionado (Editor Only)")]
    void SpawnPontoSelecionado()
    {
        #if UNITY_EDITOR
        var sel = UnityEditor.Selection.activeGameObject;
        if (sel == null)
        {
            Debug.LogWarning("[EventoSpawner] Nenhum GameObject selecionado no Editor.");
            return;
        }
        var ponto = sel.GetComponent<EventoLocalPoint>();
        if (ponto == null)
        {
            Debug.LogWarning("[EventoSpawner] Objeto selecionado não tem EventoLocalPoint.");
            return;
        }
        SpawnParaPonto(ponto);
        #endif
    }

    void SpawnParaPonto(EventoLocalPoint ponto)
    {
        if (ponto == null || !ponto.habilitado) return;
        if (locaisBloqueados.Contains(ponto.local))
        {
            if (logDetalhado)
                Debug.Log($"[EventoSpawner] (Manual) Local {ponto.local} bloqueado; aguardando resolução.");
            return;
        }
        var localAlvo = ponto.local;
        var candidatos = (eventosDisponiveis ?? new Evento[0])
            .Where(e => e != null && e.local == localAlvo)
            .ToArray();
        if (candidatos.Length == 0)
        {
            Debug.LogWarning($"[EventoSpawner] (Manual) Ponto '{ponto.name}' sem eventos para local {localAlvo}.");
            return;
        }
        var eventoAleatorio = candidatos[Random.Range(0, candidatos.Length)];
        GameObject prefabIcone = ponto.prefabIconeOverride;
        if (prefabIcone == null)
        {
            if (iconePrefabs == null || iconePrefabs.Length == 0)
            {
                Debug.LogError("[EventoSpawner] (Manual) Lista global de ícones vazia.");
                return;
            }
            var listaCompatíveis = iconePrefabs
                .Where(p => p != null)
                .Select(p => new { prefab = p, clic = p.GetComponent<EventoIconeClick>() })
                .Where(pc => pc.clic != null)
                .Where(pc => pc.clic.aceitaQualquerLocal || pc.clic.localIcone == localAlvo)
                .Select(pc => pc.prefab)
                .ToList();
            prefabIcone = listaCompatíveis.Count > 0 ? listaCompatíveis[Random.Range(0, listaCompatíveis.Count)] : iconePrefabs[Random.Range(0, iconePrefabs.Length)];
        }
        if (prefabIcone == null)
        {
            Debug.LogError($"[EventoSpawner] (Manual) Prefab nulo para ponto '{ponto.name}'.");
            return;
        }
        Vector3 spawnPos = ponto.transform.position + ponto.spawnOffset;
        SpawnIconeEmPonto(ponto, prefabIcone, eventoAleatorio, true);
    }

    // Spawna por local (escolhe ponto aleatório daquela localidade)
    void SpawnParaLocal(EventoLocal local)
    {
        if (locaisBloqueados.Contains(local)) return;
        var todos = UnityEngine.Object.FindObjectsByType<EventoLocalPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (logDetalhado)
        {
            Debug.Log($"[EventoSpawner] SpawnParaLocal({local}) - total pontos encontrados={todos.Length}");
            foreach (var pt in todos)
            {
                if (pt == null) continue;
                Debug.Log($"[EventoSpawner] PONTO LISTAGEM name='{pt.name}' ativoGO={pt.gameObject.activeSelf} habilitado={pt.habilitado} local={pt.local}");
            }
        }
        var pontos = todos
            .Where(p => p != null && p.habilitado && p.local.Equals(local))
            .ToList();
        if (pontos.Count == 0)
        {
            if (logDetalhado)
                Debug.LogWarning($"[EventoSpawner] Nenhum ponto habilitado para local {local}.");
            return;
        }
        var pontoEscolhido = pontos[Random.Range(0, pontos.Count)];
        if (logDetalhado)
            Debug.Log($"[EventoSpawner] SpawnParaLocal local={local}: ponto escolhido='{pontoEscolhido.name}'");
        // Escolhe evento
        var candidatos = (eventosDisponiveis ?? new Evento[0])
            .Where(e => e != null && e.local == local)
            .ToArray();
        if (candidatos.Length == 0)
        {
            if (logDetalhado)
                Debug.LogWarning($"[EventoSpawner] Sem eventos para local {local}.");
            return;
        }
        var eventoAleatorio = candidatos[Random.Range(0, candidatos.Length)];
        if (logDetalhado)
            Debug.Log($"[EventoSpawner] Evento escolhido para local {local}: '{eventoAleatorio?.name}' (candidatos={candidatos.Length})");
        // Escolhe prefab
        GameObject prefabIcone = pontoEscolhido.prefabIconeOverride;
        if (prefabIcone == null)
        {
            var listaCompatíveis = (iconePrefabs ?? new GameObject[0])
                .Where(p => p != null)
                .Select(p => new { prefab = p, clic = p.GetComponent<EventoIconeClick>() })
                .Where(pc => pc.clic != null)
                .Where(pc => pc.clic.aceitaQualquerLocal || pc.clic.localIcone == local)
                .Select(pc => pc.prefab)
                .ToList();
            if (logDetalhado)
                Debug.Log($"[EventoSpawner] Prefabs compatíveis para local {local}: {listaCompatíveis.Count}");
            prefabIcone = listaCompatíveis.Count > 0 ? listaCompatíveis[Random.Range(0, listaCompatíveis.Count)] : (iconePrefabs != null && iconePrefabs.Length > 0 ? iconePrefabs[Random.Range(0, iconePrefabs.Length)] : null);
        }
        if (prefabIcone == null)
        {
            Debug.LogError($"[EventoSpawner] Nenhum prefab disponível para local {local}.");
            return;
        }
        SpawnIconeEmPonto(pontoEscolhido, prefabIcone, eventoAleatorio);
    }

    void SpawnIconeEmPonto(EventoLocalPoint ponto, GameObject prefabIcone, Evento eventoAleatorio, bool manual = false)
    {
        if (ponto == null || prefabIcone == null || eventoAleatorio == null) return;
        var localAlvo = ponto.local;
        Vector3 spawnPos = ponto.transform.position + ponto.spawnOffset;
        var iconeInstanciado = Instantiate(prefabIcone, spawnPos, Quaternion.identity, ponto.transform);
        if (logDetalhado)
            Debug.Log($"[EventoSpawner] {(manual ? "(Manual) " : "")}Spawn ponto='{ponto.name}' prefab='{prefabIcone.name}' evento='{eventoAleatorio.name}' local={localAlvo}");
        if (iconeInstanciado.GetComponent<Collider2D>() == null)
            iconeInstanciado.AddComponent<BoxCollider2D>();
        var click = iconeInstanciado.GetComponent<EventoIconeClick>();
        if (click != null)
        {
            if (click.aceitaQualquerLocal && ponto.especializarWildcard)
                click.localIcone = localAlvo;
            click.DefinirEvento(eventoAleatorio);
            if (!click.aceitaQualquerLocal && click.localIcone != localAlvo && logDetalhado)
                Debug.LogWarning($"[EventoSpawner] Ícone '{iconeInstanciado.name}' possui localIcone={click.localIcone} diferente de local do evento={localAlvo} (pode causar incoerência visual).");
        }
        var binder = iconeInstanciado.GetComponent<EventoIconeOrigemBinder>() ?? iconeInstanciado.AddComponent<EventoIconeOrigemBinder>();
        binder.prefabRef = prefabIcone;
        binder.eventoRef = eventoAleatorio;
        binder.RegistrarAgora();
        if (iconeRespawnPrefab != null)
        {
            var respawnData = iconeInstanciado.GetComponent<EventoIconeRespawnData>() ?? iconeInstanciado.AddComponent<EventoIconeRespawnData>();
            respawnData.respawnPrefab = iconeRespawnPrefab;
            respawnData.parent = ponto.transform;
            respawnData.worldPosition = spawnPos;
            respawnData.worldRotation = Quaternion.identity;
        }
        // Bloqueia local até resolução
        locaisBloqueados.Add(localAlvo);
        if (iconeAtualPorLocal.ContainsKey(localAlvo)) iconeAtualPorLocal[localAlvo] = iconeInstanciado; else iconeAtualPorLocal.Add(localAlvo, iconeInstanciado);
    }

    // Context menu para diagnosticar especificamente a localidade Floresta
    [ContextMenu("Forçar Spawn Floresta (Diagnóstico)")]
    void ForcarSpawnFlorestaDiagnostico()
    {
        Debug.Log("[EventoSpawner] Forçar Spawn Floresta (Diagnóstico) acionado.");
        SpawnParaLocal(EventoLocal.Floresta);
    }

    // Extra: listar pontos por cada localidade sem tentar spawn (menu de contexto)
    [ContextMenu("Listar Pontos Por Local (Diagnóstico)")]
    void ListarPontosPorLocalDiagnostico()
    {
        var todos = UnityEngine.Object.FindObjectsByType<EventoLocalPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[EventoSpawner] ListarPontos: total={todos.Length}");
        var grupos = todos.Where(p=>p!=null).GroupBy(p=>p.local);
        foreach (var g in grupos)
        {
            Debug.Log($"[EventoSpawner] Local={g.Key} count={g.Count()} ativos={g.Count(p=>p.gameObject.activeSelf)} habilitados={g.Count(p=>p.habilitado)}");
            foreach (var pt in g)
            {
                Debug.Log($"    - name='{pt.name}' ativoGO={pt.gameObject.activeSelf} habilitado={pt.habilitado} offset={pt.spawnOffset}");
            }
        }
    }
}
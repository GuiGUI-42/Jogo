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

    [Header("Debug Contagem")]
    [Tooltip("Logar contagem de spawns/respawns por local para depuração.")]
    public bool logContagemRespawn = true;

    [Header("Sequencial por Localidade")]
    [Tooltip("Se verdadeiro, spawna um ícone por localidade em sequência ao iniciar.")]
    public bool spawnSequencialPorLocalidade = true;
    [Tooltip("Intervalo entre spawns de localidades diferentes (segundos).")]
    public float delayEntreLocalidades = 5f;
    [Tooltip("Delay para respawn após resolver evento daquele local.")]
    public float delayRespawnPorLocal = 2f;

    [Header("Watchdog Looping")]
    [Tooltip("Ativa verificação contínua para garantir respawn infinito.")]
    public bool usarWatchdog = true;
    [Tooltip("Intervalo entre varreduras do watchdog (segundos).")]
    public float watchdogInterval = 10f;
    [Tooltip("Tempo máximo bloqueado antes de forçar respawn (segundos). 0 desativa timeout.")]
    public float watchdogTimeout = 60f;
    [Tooltip("Destruir ícone antigo quando ocorrer timeout.")]
    public bool watchdogDestruirIconeTimeout = true;

    // Controle de bloqueio por local (um evento ativo/ícone pendente por local)
    readonly HashSet<EventoLocal> locaisBloqueados = new HashSet<EventoLocal>();
    // Ícone atualmente spawnado por local (para verificação extra / limpeza futura)
    readonly Dictionary<EventoLocal, GameObject> iconeAtualPorLocal = new Dictionary<EventoLocal, GameObject>();
    // Timestamp de quando o local foi bloqueado
    readonly Dictionary<EventoLocal, float> tempoBloqueioInicio = new Dictionary<EventoLocal, float>();
    // Momento futuro (Time.time) em que um respawn manual/planejado poderá ocorrer (delay de aceite da Intro)
    readonly Dictionary<EventoLocal, float> respawnAgendadoAposTempo = new Dictionary<EventoLocal, float>();

    // Contadores para debug
    readonly Dictionary<EventoLocal, int> contagemSpawnInicialPorLocal = new Dictionary<EventoLocal, int>();
    readonly Dictionary<EventoLocal, int> contagemRespawnPorLocal = new Dictionary<EventoLocal, int>();

    // Evento exclusivo por local durante o ciclo de vida (até finalizar combate/passivo)
    readonly Dictionary<EventoLocal, Evento> eventoExclusivoPorLocal = new Dictionary<EventoLocal, Evento>();

    Coroutine watchdogCoroutine;

    void OnEnable()
    {
        EventoUI.OnEventoResolvido += HandleEventoResolvido;
    }

    void OnDisable()
    {
        EventoUI.OnEventoResolvido -= HandleEventoResolvido;
        if (watchdogCoroutine != null)
        {
            StopCoroutine(watchdogCoroutine);
            watchdogCoroutine = null;
        }
    }

    void Start()
    {
        if (spawnSequencialPorLocalidade)
            StartCoroutine(SpawnSequencialInicial());
        else
            StartCoroutine(SpawnTodosAposDelay());

        if (usarWatchdog && watchdogCoroutine == null)
            watchdogCoroutine = StartCoroutine(WatchdogLoop());
    }

    void HandleEventoResolvido(EventoLocal local, Evento evento)
    {
        if (locaisBloqueados.Contains(local))
        {
            if (logDetalhado)
                Debug.Log($"[EventoSpawner] Evento resolvido para local {local}. Liberando para próximo spawn.");
            locaisBloqueados.Remove(local);
        }
        tempoBloqueioInicio.Remove(local);
        respawnAgendadoAposTempo.Remove(local);
        eventoExclusivoPorLocal.Remove(local); // libera vínculo exclusivo do evento para este local
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
        tempoBloqueioInicio[localAlvo] = Time.time;
        // Se havia um respawn agendado para depois desse momento, já cumpriu; remove para evitar interferência.
        respawnAgendadoAposTempo.Remove(localAlvo);

        // Define vínculo exclusivo do evento para o local até finalizar o evento
        eventoExclusivoPorLocal[localAlvo] = eventoAleatorio;

        // Contagem de spawn inicial (debug)
        if (!contagemSpawnInicialPorLocal.ContainsKey(localAlvo)) contagemSpawnInicialPorLocal[localAlvo] = 0;
        contagemSpawnInicialPorLocal[localAlvo]++;
        if (logContagemRespawn)
            Debug.Log($"[EventoSpawner][Debug] Spawn inicial local={localAlvo} total={contagemSpawnInicialPorLocal[localAlvo]}");
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

    IEnumerator WatchdogLoop()
    {
        while (usarWatchdog)
        {
            float intervalo = watchdogInterval > 0f ? watchdogInterval : 1f;
            yield return new WaitForSeconds(intervalo);
            foreach (var local in locaisBloqueados.ToArray())
            {
                iconeAtualPorLocal.TryGetValue(local, out var go);
                bool ativo = go != null && go.activeSelf;
                if (!ativo)
                {
                    // Se existe um respawn agendado e o tempo ainda não chegou, apenas continuar aguardando.
                    if (respawnAgendadoAposTempo.TryGetValue(local, out var tResp) && Time.time < tResp)
                    {
                        if (logDetalhado)
                            Debug.Log($"[EventoSpawner][Watchdog] Aguardando respawn agendado para local {local} (faltam {tResp - Time.time:0.0}s).");
                        continue;
                    }
                    if (!tempoBloqueioInicio.ContainsKey(local)) tempoBloqueioInicio[local] = Time.time;
                    var elapsedSemIcone = Time.time - tempoBloqueioInicio[local];
                    // Sem ícone e sem respawn agendado: manter bloqueado, a não ser que estoure timeout
                    if (watchdogTimeout > 0f && elapsedSemIcone >= watchdogTimeout)
                    {
                        if (logDetalhado)
                            Debug.Log($"[EventoSpawner][Watchdog] Timeout {elapsedSemIcone:0.0}s sem ícone no local {local}. Forçando respawn do MESMO evento.");
                        if (watchdogDestruirIconeTimeout && go != null)
                            Destroy(go);
                        // Não desbloqueia: respawna o mesmo evento e mantém exclusividade
                        tempoBloqueioInicio[local] = Time.time; // reinicia janela
                        respawnAgendadoAposTempo.Remove(local);
                        RespawnMesmoEventoNoLocal(local);
                    }
                    else
                    {
                        if (logDetalhado)
                            Debug.Log($"[EventoSpawner][Watchdog] Local {local} bloqueado sem ícone (aguardando fluxo da UI).");
                    }
                    continue;
                }
                if (!tempoBloqueioInicio.ContainsKey(local)) tempoBloqueioInicio[local] = Time.time;
                if (watchdogTimeout > 0f && tempoBloqueioInicio.TryGetValue(local, out var t0))
                {
                    var elapsed = Time.time - t0;
                    if (elapsed >= watchdogTimeout)
                    {
                        // Respeita respawn agendado: só forçar se já passou do tempo previsto
                        if (respawnAgendadoAposTempo.TryGetValue(local, out var tResp2) && Time.time < tResp2)
                        {
                            if (logDetalhado)
                                Debug.Log($"[EventoSpawner][Watchdog] Timeout atingido mas aguardando respawn agendado futuro para local {local}." );
                            continue;
                        }
                        if (logDetalhado)
                            Debug.Log($"[EventoSpawner][Watchdog] Timeout {elapsed:0.0}s em local {local}. Forçando respawn do MESMO evento.");
                        if (watchdogDestruirIconeTimeout && go != null)
                            Destroy(go);
                        // Mantém bloqueio e respawna o mesmo evento
                        tempoBloqueioInicio[local] = Time.time;
                        respawnAgendadoAposTempo.Remove(local);
                        RespawnMesmoEventoNoLocal(local);
                    }
                }
            }
        }
    }

    // Recria um ícone para o mesmo evento exclusivo do local (sem liberar bloqueio)
    void RespawnMesmoEventoNoLocal(EventoLocal local)
    {
        if (!eventoExclusivoPorLocal.TryGetValue(local, out var eventoFixo) || eventoFixo == null)
        {
            Debug.LogWarning($"[EventoSpawner] RespawnMesmoEventoNoLocal: nenhum evento fixo registrado para {local}. Abortando respawn dedicado.");
            return;
        }

        // Escolhe um ponto válido para o local
        var todos = UnityEngine.Object.FindObjectsByType<EventoLocalPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var pontos = todos.Where(p => p != null && p.habilitado && p.local.Equals(local)).ToList();
        if (pontos.Count == 0)
        {
            Debug.LogWarning($"[EventoSpawner] RespawnMesmoEventoNoLocal: nenhum ponto habilitado para {local}.");
            return;
        }
        var pontoEscolhido = pontos[Random.Range(0, pontos.Count)];

        // Prefab de ícone compatível
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
            prefabIcone = listaCompatíveis.Count > 0 ? listaCompatíveis[Random.Range(0, listaCompatíveis.Count)] : (iconePrefabs != null && iconePrefabs.Length > 0 ? iconePrefabs[Random.Range(0, iconePrefabs.Length)] : null);
        }
        if (prefabIcone == null)
        {
            Debug.LogError($"[EventoSpawner] RespawnMesmoEventoNoLocal: nenhum prefab disponível para {local}.");
            return;
        }

        // Spawn direto (mesmo evento), preservando bloqueio
        SpawnIconeEmPonto(pontoEscolhido, prefabIcone, eventoFixo, false);
    }

    // Chamado pela UI ao aceitar evento e agendar respawn do ícone de reabrir
    public void AgendarRespawnLocal(EventoLocal local, float delaySeg)
    {
        if (delaySeg <= 0f)
        {
            // Se delay zero, não agenda; watchdog pode reutilizar lógica padrão
            respawnAgendadoAposTempo.Remove(local);
            return;
        }
        var t = Time.time + delaySeg;
        respawnAgendadoAposTempo[local] = t;
        if (logDetalhado)
            Debug.Log($"[EventoSpawner] Respawn agendado para local {local} em {delaySeg:0.##}s (t={t:0.##}).");
    }

    // Permite que a UI registre um novo ícone (reabrir) para um local bloqueado sem que o watchdog force outro
    public void RegistrarIconeLocal(EventoLocal local, GameObject iconGO)
    {
        if (iconGO == null) return;
        if (!locaisBloqueados.Contains(local))
            locaisBloqueados.Add(local); // garante estado consistente
        iconeAtualPorLocal[local] = iconGO;
        tempoBloqueioInicio[local] = Time.time; // reinicia contagem
        // Respawn cumprido
        respawnAgendadoAposTempo.Remove(local);
        if (logDetalhado)
            Debug.Log($"[EventoSpawner] Ícone registrado manualmente para local {local}: '{iconGO.name}'.");

        // Atualiza/afirma o vínculo do evento exclusivo com base no componente do ícone
        // Preferimos dados do ícone de reabrir (são garantidos para respawn), e só caímos no binder para ícones originais
        Evento eventoVinculado = null;
        var reabrir = iconGO.GetComponent<EventoReabrirIcon>();
        if (reabrir != null && reabrir.evento != null) eventoVinculado = reabrir.evento;
        if (eventoVinculado == null)
        {
            var binder = iconGO.GetComponent<EventoIconeOrigemBinder>();
            if (binder != null && binder.eventoRef != null) eventoVinculado = binder.eventoRef;
        }
        if (eventoVinculado != null)
            eventoExclusivoPorLocal[local] = eventoVinculado;

        // Contagem de respawn (debug)
        if (!contagemRespawnPorLocal.ContainsKey(local)) contagemRespawnPorLocal[local] = 0;
        contagemRespawnPorLocal[local]++;
        if (logContagemRespawn)
            Debug.Log($"[EventoSpawner][Debug] Respawn local={local} total={contagemRespawnPorLocal[local]}");
    }
}
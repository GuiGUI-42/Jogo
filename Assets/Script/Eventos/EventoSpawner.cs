using UnityEngine;
using System.Collections;
using System.Linq;

public class EventoSpawner : MonoBehaviour
{
    [Header("Eventos")]
    public Evento[] eventosDisponiveis;
    [Tooltip("Se verdadeiro, ignora 'localFiltro' e escolhe aleatoriamente entre os locais configurados em 'locaisAleatorios'.")] public bool escolherLocalAleatorio = true;
    [Tooltip("Lista de locais possíveis quando 'escolherLocalAleatorio' está ativo.")] public EventoLocal[] locaisAleatorios = new EventoLocal[] { EventoLocal.Cidade, EventoLocal.Floresta };
    [Tooltip("Usado apenas se 'escolherLocalAleatorio' estiver desativado.")] public EventoLocal localFiltro = EventoLocal.Floresta;

    [Header("Ícones")]
    [Tooltip("Lista de prefabs de ícones possíveis. Um será sorteado para spawn.")]
    public GameObject[] iconePrefabs;
    [Tooltip("Prefab do ícone que poderá respawnar depois, no mesmo local do primeiro ícone (configuraremos as condições depois).")]
    public GameObject iconeRespawnPrefab;

    [Header("Spawn")]
    [Tooltip("Ponto base onde o ícone será instanciado (ex.: casa, centro da floresta)")]
    public Transform casaTransform;
    public Vector3 offset = new Vector3(5f, 7f, 0f);
    [Tooltip("Aguardar esse tempo antes de spawnar o ícone")]
    public float delayInicial = 2f;

    void Start()
    {
        StartCoroutine(SpawnAposDelay());
    }

    IEnumerator SpawnAposDelay()
    {
        if (delayInicial > 0f) yield return new WaitForSeconds(delayInicial);

        if (!casaTransform)
        {
            Debug.LogError("EventoSpawner: 'casaTransform' (ponto base) não atribuído.");
            yield break;
        }

        // Determina o local alvo
        EventoLocal localAlvo;
        if (escolherLocalAleatorio)
        {
            var poolLocais = (locaisAleatorios != null && locaisAleatorios.Length > 0) ? locaisAleatorios : new EventoLocal[] { localFiltro };
            localAlvo = poolLocais[Random.Range(0, poolLocais.Length)];
        }
        else
        {
            localAlvo = localFiltro;
        }

        // Filtra eventos pelo local alvo
        var candidatos = (eventosDisponiveis ?? new Evento[0])
            .Where(e => e != null && e.local == localAlvo)
            .ToArray();
        if (candidatos.Length == 0)
        {
            Debug.LogWarning($"EventoSpawner: nenhum Evento com local={localAlvo} disponível.");
            yield break;
        }
        var eventoAleatorio = candidatos[Random.Range(0, candidatos.Length)];

        // Filtra e escolhe um prefab de ícone compatível com o local
        if (iconePrefabs == null || iconePrefabs.Length == 0)
        {
            Debug.LogError("EventoSpawner: 'iconePrefabs' vazio. Arraste os prefabs de ícone.");
            yield break;
        }

        var listaCompatíveis = iconePrefabs
            .Where(p => p != null)
            .Select(p => new { prefab = p, clic = p.GetComponent<EventoIconeClick>() })
            .Where(pc => pc.clic != null)
            .Where(pc => pc.clic.aceitaQualquerLocal || pc.clic.localIcone == localAlvo)
            .Select(pc => pc.prefab)
            .ToList();

        GameObject prefabIcone;
        if (listaCompatíveis.Count > 0)
        {
            prefabIcone = listaCompatíveis[Random.Range(0, listaCompatíveis.Count)];
        }
        else
        {
            // Fallback: qualquer um
            Debug.LogWarning($"EventoSpawner: nenhum ícone com local compatível ({localAlvo}); usando qualquer prefab.");
            prefabIcone = iconePrefabs[Random.Range(0, iconePrefabs.Length)];
            if (!prefabIcone)
            {
                Debug.LogError("EventoSpawner: prefab selecionado nulo no fallback.");
                yield break;
            }
        }

        Vector3 spawnPos = casaTransform.position + offset;
        var iconeInstanciado = Instantiate(prefabIcone, spawnPos, Quaternion.identity, casaTransform);
        Debug.Log($"[EventoSpawner] Ícone spawnado: prefab='{prefabIcone.name}', pos={spawnPos}, parent='{casaTransform.name}', local={localAlvo}, evento='{eventoAleatorio?.name}'");

        // Garante que tem BoxCollider2D
        if (iconeInstanciado.GetComponent<Collider2D>() == null)
            iconeInstanciado.AddComponent<BoxCollider2D>();

        // Passa o evento para o ícone
        var click = iconeInstanciado.GetComponent<EventoIconeClick>();
        if (click != null)
        {
            if (click.aceitaQualquerLocal)
                click.localIcone = localAlvo; // especializa wildcard no momento do spawn
            click.DefinirEvento(eventoAleatorio);
            Debug.Log($"[EventoSpawner] Evento '{eventoAleatorio?.name}' atribuído ao ícone '{iconeInstanciado.name}'");
        }
        else
        {
            Debug.LogError("Prefab do ícone não tem o componente EventoIconeClick.");
        }

        // Anexa/atualiza binder de origem para o EventoUI saber onde respawnar
        var binder = iconeInstanciado.GetComponent<EventoIconeOrigemBinder>();
        if (binder == null) binder = iconeInstanciado.AddComponent<EventoIconeOrigemBinder>();
        binder.prefabRef = prefabIcone;
        binder.eventoRef = eventoAleatorio;
        binder.RegistrarAgora();

        // Armazena dados de respawn no próprio ícone para uso futuro (condições serão tratadas depois)
        if (iconeRespawnPrefab != null)
        {
            var respawnData = iconeInstanciado.GetComponent<EventoIconeRespawnData>();
            if (respawnData == null) respawnData = iconeInstanciado.AddComponent<EventoIconeRespawnData>();
            respawnData.respawnPrefab = iconeRespawnPrefab;
            respawnData.parent = casaTransform;
            respawnData.worldPosition = spawnPos;
            respawnData.worldRotation = Quaternion.identity;
        }
    }
}
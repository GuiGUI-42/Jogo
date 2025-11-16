using UnityEngine;

// Marca um GameObject na cena como ponto de spawn para eventos de um local específico.
// O novo EventoSpawner central localizará todos os pontos e criará ícones de acordo
// com o EventoLocal definido aqui.
public class EventoLocalPoint : MonoBehaviour
{
    [Header("Configuração do Local")]
    [Tooltip("Localidade associada a este ponto de spawn.")]
    public EventoLocal local;

    [Tooltip("Offset aplicado à posição do GameObject para instanciar o ícone.")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Se desmarcado, este ponto será ignorado pelo spawner central.")]
    public bool habilitado = true;

    [Tooltip("Se verdadeiro e o ícone sorteado for wildcard (aceitaQualquerLocal), o local do ícone será ajustado para este local.")]
    public bool especializarWildcard = true;

    [Header("Opções futuras")]
    [Tooltip("Se definido, força usar este prefab de ícone em vez de escolher na lista global do spawner.")]
    public GameObject prefabIconeOverride;

    // Poderemos expandir (ex.: limite de ícones simultâneos) futuramente.
}
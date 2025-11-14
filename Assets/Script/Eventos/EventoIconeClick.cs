using UnityEngine;
using UnityEngine.EventSystems;

public class EventoIconeClick : MonoBehaviour, IPointerClickHandler
{
    public Evento evento;
    [Tooltip("Se verdadeiro, o ícone se desativa após abrir o evento.")] public bool esconderAoAbrir = true;
    [Header("Local do Ícone")]
    [Tooltip("Local associado explicitamente a este ícone (use se 'aceitaQualquerLocal' estiver desmarcado)." )]
    public EventoLocal localIcone;

    [Tooltip("Se marcado, este ícone pode ser usado para qualquer local (wildcard). O Spawner ajustará o localIcone ao spawn.")]
    public bool aceitaQualquerLocal = true;

    [Tooltip("Se verdadeiro e 'aceitaQualquerLocal' estiver marcado, ao definir o Evento o ícone assume o local do Evento.")]
    public bool herdarLocalDoEventoSeWildcard = true;

    // Reposto para o EventoSpawner configurar o evento deste ícone
    public void DefinirEvento(Evento e)
    {
        evento = e;
        if (evento != null && aceitaQualquerLocal && herdarLocalDoEventoSeWildcard)
            localIcone = evento.local;
        Debug.Log($"[IconeClick] DefinirEvento: nome='{evento?.name}', local='{localIcone}', go='{name}'");
    }

    void OnEnable()
    {
        // Garante que cliques por EventSystem funcionem em 2D
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
        {
            cam.gameObject.AddComponent<Physics2DRaycaster>();
            Debug.Log("[IconeClick] Physics2DRaycaster adicionado na Camera.main");
        }
        else if (cam == null)
        {
            Debug.LogWarning("[IconeClick] Camera.main não encontrada; OnPointerClick pode não funcionar. Usando OnMouseDown.");
        }
    }

    void OnMouseDown()
    {
        AbrirPeloClique();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AbrirPeloClique();
    }

    void AbrirPeloClique()
    {
        Debug.Log($"[IconeClick] Clique em '{name}'. Evento atribuído='{evento?.name}'");
        if (!evento)
        {
            Debug.LogError("EventoIconeClick: evento não definido.");
            return;
        }

        var ui = EventoUI.Instance ?? EventoUI.EnsureInstance();
        if (ui == null)
        {
            Debug.LogError("EventoUI não encontrado na cena.");
            return;
        }

        Debug.Log("[IconeClick] Chamando EventoUI.Abrir(evento)");
        ui.Abrir(evento);
        Debug.Log("[IconeClick] EventoUI.Abrir concluído");

        if (esconderAoAbrir)
            gameObject.SetActive(false);

        // Oculta todos os outros ícones depois de abrir o evento
        OcultarTodosIcones();
    }

    public static void OcultarTodosIcones()
    {
        var todos = UnityEngine.Object.FindObjectsByType<EventoIconeClick>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ic in todos)
        {
            if (ic != null && ic.gameObject.activeSelf)
                ic.gameObject.SetActive(false);
        }
    }
}
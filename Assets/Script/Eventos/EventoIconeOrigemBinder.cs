using UnityEngine;

// Anexe esse script ao prefab do ícone (mundo). Ele registra no EventoUI
// a origem (transform), o prefab e o evento associado, para que o reabrir
// respawne no mesmo lugar. Também registra no clique por redundância.
[RequireComponent(typeof(Transform))]
public class EventoIconeOrigemBinder : MonoBehaviour
{
    [Tooltip("Prefab deste ícone, usado para respawn.")]
    public GameObject prefabRef;
    [Tooltip("Evento associado a este ícone.")]
    public Evento eventoRef;

    public void RegistrarAgora()
    {
        var ui = EventoUI.EnsureInstance();
        if (ui != null)
        {
            ui.DefinirUltimaOrigemIcone(transform, prefabRef, eventoRef);
            Debug.Log($"[Binder] Registrado origem='{name}', prefab='{prefabRef?.name}', evento='{eventoRef?.name}'");
        }
        else
        {
            Debug.LogWarning("[Binder] EventoUI não encontrado ao registrar origem.");
        }
    }

    void OnMouseDown()
    {
        // Quando o jogador clicar no ícone, reforça o registro da origem
        RegistrarAgora();
    }
}

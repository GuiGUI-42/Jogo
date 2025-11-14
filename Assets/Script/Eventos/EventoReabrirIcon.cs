using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ícone simples que, ao ser clicado, reabre o evento na fase de opções.
public class EventoReabrirIcon : MonoBehaviour, IPointerClickHandler
{
    EventoUI eventoUI;

    public Image heroSlotPreview; // opcional: mostrar retrato do herói
    public TMPro.TMP_Text tituloPreview; // opcional: mostrar título do evento

    public void Configurar(EventoUI ui)
    {
        eventoUI = ui;
        AtualizarPreview();
    }

    void Awake()
    {
        if (eventoUI == null)
        {
            eventoUI = EventoUI.EnsureInstance();
            AtualizarPreview();
        }
    }

    void AtualizarPreview()
    {
        if (eventoUI == null) return;
        if (heroSlotPreview != null && eventoUI.heroiSelecionado != null)
        {
            var sprite = eventoUI.heroiSelecionado.GetSpriteRetrato();
            if (sprite != null)
            {
                heroSlotPreview.sprite = sprite;
                heroSlotPreview.preserveAspect = true;
                heroSlotPreview.color = Color.white;
            }
        }
        if (tituloPreview != null && eventoUI != null)
        {
            tituloPreview.text = eventoUI.name; // ou eventoUI.eventoAtual?.nomeEvento se quiser
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventoUI == null) eventoUI = EventoUI.EnsureInstance();
        if (eventoUI == null) { Debug.LogWarning("[EventoReabrirIcon] eventoUI nulo ao clicar."); return; }
        Debug.Log("[EventoReabrirIcon] Clique recebido. Solicitando reabertura em Opcoes.");
        eventoUI.AbrirOpcoesFromIcon(gameObject);
    }

    void OnMouseDown()
    {
        // Suporte a ícones de mundo (SpriteRenderer + Collider2D), sem UI
        if (eventoUI == null) eventoUI = EventoUI.EnsureInstance();
        if (eventoUI == null) { Debug.LogWarning("[EventoReabrirIcon] eventoUI nulo no OnMouseDown."); return; }
        Debug.Log("[EventoReabrirIcon] OnMouseDown recebido (mundo). Reabrindo.");
        eventoUI.AbrirOpcoesFromIcon(gameObject);
    }
}

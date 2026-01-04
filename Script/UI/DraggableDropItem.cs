using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System; 

public class DraggableDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Payload do Drop")]
    public Item item; // <-- CAMPO RESTAURADO (Resolve os erros CS1061)
    public ScriptableObject asset; // ItemCombate ou outro asset
    public int quantidade = 1;

    [Header("Referências")]
    public Image sourceImage;

    // --- DELEGATES PARA A LOJA E COMBATE ---
    // Uma função que retorna Bool. Se retornar FALSE, o drag é cancelado.
    public Func<bool> VerificarCondicaoDeArraste; 
    
    // Evento de sucesso 
    public event Action OnItemArrastadoComSucesso;
    // ---------------------------------------

    Canvas canvas;
    GameObject dragIcon;
    CanvasGroup sourceCanvasGroup;

    void Awake()
    {
        if (!sourceImage) sourceImage = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            var c = UnityEngine.Object.FindFirstObjectByType<Canvas>(); // Unity 2023+
            if (!c) c = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (c) canvas = c;
        }
        sourceCanvasGroup = GetComponent<CanvasGroup>();
        if (!sourceCanvasGroup) sourceCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. VERIFICAÇÃO EXTERNA (LOJA)
        // Se houver uma condição configurada e ela retornar FALSE, cancela tudo.
        if (VerificarCondicaoDeArraste != null)
        {
            if (!VerificarCondicaoDeArraste.Invoke())
            {
                Debug.Log("[DraggableDropItem] Arraste bloqueado pela condição externa (Ex: Sem Ouro).");
                return;
            }
        }

        if (sourceImage == null || canvas == null) return;
        // Garante que tem pelo menos algum dado para arrastar
        if (item == null && asset == null) return;

        // Cria o ícone fantasma
        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvas.transform, false);
        
        var img = dragIcon.GetComponent<Image>();
        img.sprite = sourceImage.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        
        (dragIcon.transform as RectTransform).sizeDelta = (sourceImage.transform as RectTransform).rect.size;

        sourceCanvasGroup.alpha = 0.6f;
        sourceCanvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        sourceCanvasGroup.alpha = 1f;
        sourceCanvasGroup.blocksRaycasts = true;
    }

    public void NotificarSucesso()
    {
        Debug.Log("[DraggableDropItem] Sucesso notificado! Item entregue.");
        OnItemArrastadoComSucesso?.Invoke();
    }
}
using UnityEngine;
using UnityEngine.UI;

public class BagUI : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Painel que contém os slots de itens (ex.: SlotItems)")]
    public GameObject slotItemsPanel;

    [Tooltip("Botão/área clicável do ícone da Bag. Se vazio, tenta pegar do próprio GameObject.")]
    public Button bagButton;

    [Header("Comportamento")]
    [Tooltip("Se verdadeiro, o painel inicia oculto.")]
    public bool startHidden = true;

    void Awake()
    {
        if (bagButton == null)
            bagButton = GetComponent<Button>();

        if (slotItemsPanel != null && startHidden)
            slotItemsPanel.SetActive(false);

        if (bagButton != null)
        {
            bagButton.onClick.RemoveListener(ToggleBag);
            bagButton.onClick.AddListener(ToggleBag);

            // Garante que o ícone da Bag aceita drops
            if (bagButton.gameObject.GetComponent<BagDropTarget>() == null)
                bagButton.gameObject.AddComponent<BagDropTarget>();
        }
        else
        {
            Debug.LogWarning("[BagUI] Nenhum Button encontrado. Adicione um Button ao ícone da Bag ou preencha o campo 'bagButton'.");
        }
    }

    public void ToggleBag()
    {
        if (slotItemsPanel == null)
        {
            Debug.LogWarning("[BagUI] 'slotItemsPanel' não atribuído.");
            return;
        }
        bool abrir = !slotItemsPanel.activeSelf;
        slotItemsPanel.SetActive(abrir);
        if (abrir)
        {
            var invUI = slotItemsPanel.GetComponent<BagInventoryUI>();
            if (invUI) invUI.RefreshSlots();
        }
    }

    public void Open()
    {
        if (slotItemsPanel != null)
        {
            slotItemsPanel.SetActive(true);
            var invUI = slotItemsPanel.GetComponent<BagInventoryUI>();
            if (invUI) invUI.RefreshSlots();
        }
    }

    public void Close()
    {
        if (slotItemsPanel != null) slotItemsPanel.SetActive(false);
    }
}

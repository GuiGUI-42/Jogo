using UnityEngine;

// Enum com Flags (Mantemos os números para uso nos Filtros de Efeito)
[System.Flags]
public enum TipoItem
{
    Nenhum = 0,
    Arma = 1 << 0,        // 1
    Utensilio = 1 << 1,   // 2
    Armadura = 1 << 2,    // 4
    Companheiro = 1 << 3, // 8
    Magia = 1 << 4        // 16 (NOVO TIPO)
}

public enum TamanhoItem
{
    UmSlot,
    DoisSlotsHorizontal, 
    DoisSlotsVertical    
}

[CreateAssetMenu(menuName = "Item/Item Genérico")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string descricao;

    [Header("Características Físicas")]
    // Se quiser que itens genéricos (não combate) também usem lista, mude aqui também:
    // public List<TipoItem> tipos = new List<TipoItem>();
    [Tooltip("Selecione um ou mais tipos.")]
    public TipoItem tipos; // Mantive como era no genérico, mas você pode mudar para List se quiser padronizar.

    [Tooltip("Define o espaço ocupado no inventário.")]
    public TamanhoItem tamanho = TamanhoItem.UmSlot;
}
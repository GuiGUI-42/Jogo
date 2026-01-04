using UnityEngine;
using UnityEngine.Localization;

[System.Flags]
public enum TipoItem
{
    Nenhum = 0,
    Arma = 1 << 0,        
    Utensilio = 1 << 1,   
    Armadura = 1 << 2,    
    Companheiro = 1 << 3, 
    Magia = 1 << 4        
}

public enum TamanhoItem
{
    UmSlot,
    DoisSlotsHorizontal, 
    DoisSlotsVertical    
}

// O enum RaridadeItem FOI REMOVIDO DAQUI pois já existe no seu projeto (ItemCombate.cs)

[CreateAssetMenu(menuName = "Item/Item Genérico")]
public class Item : ScriptableObject
{
    public LocalizedString itemName; 
    public Sprite icon;
    public LocalizedString descricao;

    [Header("Características")]
    // Usa o RaridadeItem que já existe no seu projeto
    public RaridadeItem raridade = RaridadeItem.Comum; 
    
    public TipoItem tipos; 
    public TamanhoItem tamanho = TamanhoItem.UmSlot;

    /// <summary>
    /// Calcula o valor de COMPRA na loja baseado na tabela de raridade e slots.
    /// </summary>
    public int GetValorCompra()
    {
        int valorBase = 0;

        // Tabela de base para 1 Slot
        switch (raridade)
        {
            case RaridadeItem.Comum:    valorBase = 5;  break;
            case RaridadeItem.Incomum:  valorBase = 10; break;
            case RaridadeItem.Raro:     valorBase = 15; break;
            case RaridadeItem.Epico:    valorBase = 20; break;
            case RaridadeItem.Lendario: valorBase = 30; break;
        }

        // Multiplicador de Slots (1x ou 2x)
        int multiplicador = (tamanho == TamanhoItem.UmSlot) ? 1 : 2;

        return valorBase * multiplicador;
    }

    /// <summary>
    /// Calcula o valor de VENDA (metade da compra).
    /// </summary>
    public int GetValorVenda()
    {
        return Mathf.FloorToInt(GetValorCompra() / 2f);
    }
}
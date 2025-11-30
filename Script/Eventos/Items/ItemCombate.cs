using UnityEngine;
using System.Collections.Generic;

// --- DEFINIÇÕES DE ESTRUTURAS AUXILIARES ---

[System.Serializable]
public class PorcentagemAtributos
{
    [Range(0, 500)] public float forca;
    [Range(0, 500)] public float carisma;
    [Range(0, 500)] public float sabedoria;
    [Range(0, 500)] public float inteligencia;
    [Range(0, 500)] public float vitalidade;
    [Range(0, 500)] public float destreza;
}

public enum TipoElemento
{
    Fisico,
    Fogo,      
    Gelo,      
    Eletrico,  
    Veneno     
}

public enum TipoEfeitoItem
{
    Nenhum,
    ReduzirCooldownTipo,        
    ModificarDeslocamento,      
    ModificarDescanso,          
    Atordoar,                   
    Molhar,                     
    Vulneravel,                 
    Inflamavel,                 
    UsarAdjacente,              
    ReduzirCooldownAdjacente,
    AumentarDano 
}

public enum ModoEfeito { Ativo, Passivo }
public enum AlvoEfeito { Oponente, Usuario, Adjacentes }

[System.Serializable]
public class ComponenteDano
{
    public TipoElemento tipo;
    public int danoBase;
    public PorcentagemAtributos escalaAtributos;
}

[System.Serializable]
public class EfeitoItemConfig
{
    public TipoEfeitoItem tipoEfeito;
    public ModoEfeito modo;
    public AlvoEfeito alvo;
    public float valor;
    public TipoElemento elementoAlvo; 
    [Tooltip("Soma dos valores dos Tipos (Arma=1, Utensilio=2, Magia=16...). Ex: Para afetar Arma e Magia, coloque 17.")]
    public int parametroExtra; 
}

// --- CLASSE PRINCIPAL ---

[CreateAssetMenu(menuName = "Item/ItemCombate")]
public class ItemCombate : ScriptableObject
{
    public string nomeItem;
    public Sprite iconeItem;
    [TextArea] public string descricaoItem;

    [Header("Características Físicas")]
    // MUDANÇA AQUI: Agora é uma lista para adicionar múltiplos tipos visualmente
    public List<TipoItem> tipos = new List<TipoItem>(); 
    
    public TamanhoItem tamanho = TamanhoItem.UmSlot;

    [Header("Configuração de Combate")]
    [Min(0f)] public float cooldownSegundos = 1f;
    
    [Header("Danos")]
    public List<ComponenteDano> danos = new List<ComponenteDano>();

    [Header("Cura")]
    [Min(0)] public int curaBase;
    public PorcentagemAtributos escalaCura;

    [Header("Armadura")]
    [Min(0)] public int armaduraBase;
    public PorcentagemAtributos escalaArmadura;

    [Header("Efeitos Especiais")]
    public List<EfeitoItemConfig> efeitos = new List<EfeitoItemConfig>();

    // --- Helper para compatibilidade com sistema de máscaras ---
    public bool PossuiTipo(int maskFiltro)
    {
        if (maskFiltro == 0) return true; // 0 afeta todos
        if (tipos == null) return false;

        // Verifica se ALGUM dos tipos da lista bate com a máscara do filtro
        foreach (var t in tipos)
        {
            if (((int)t & maskFiltro) != 0) return true;
        }
        return false;
    }

    public int CalcularDanoComponente(ComponenteDano componente, Heroi heroi)
    {
        if (componente == null || heroi == null) return 0;
        if (componente.escalaAtributos == null) return componente.danoBase;
        return componente.danoBase + CalcularBonusAtributos(heroi, componente.escalaAtributos);
    }

    public int CalcularCura(Heroi heroi)
    {
        if (heroi == null) return 0;
        int bonus = (escalaCura != null) ? CalcularBonusAtributos(heroi, escalaCura) : 0;
        return curaBase + bonus;
    }

    public int CalcularArmadura(Heroi heroi)
    {
        if (heroi == null) return 0;
        int bonus = (escalaArmadura != null) ? CalcularBonusAtributos(heroi, escalaArmadura) : 0;
        return armaduraBase + bonus;
    }

    private int CalcularBonusAtributos(Heroi heroi, PorcentagemAtributos escala)
    {
        return Mathf.RoundToInt(
            heroi.forca * (escala.forca / 100f) +
            heroi.carisma * (escala.carisma / 100f) +
            heroi.sabedoria * (escala.sabedoria / 100f) +
            heroi.inteligencia * (escala.inteligencia / 100f) +
            heroi.vitalidade * (escala.vitalidade / 100f) +
            heroi.destreza * (escala.destreza / 100f)
        );
    }

    public bool PodeAtivar(float ultimoUsoTime)
    {
        return (Time.time - ultimoUsoTime) >= cooldownSegundos;
    }

    public float TempoRestante(float ultimoUsoTime)
    {
        return Mathf.Max(0, cooldownSegundos - (Time.time - ultimoUsoTime));
    }
}
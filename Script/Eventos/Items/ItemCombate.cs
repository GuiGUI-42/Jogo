using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization; // Necessário para tradução

// --- DEFINIÇÕES DE ESTRUTURAS AUXILIARES ---

[System.Serializable]
public class PorcentagemAtributos
{
    [Range(0f, 10f)] public float forca;
    [Range(0f, 10f)] public float carisma;
    [Range(0f, 10f)] public float sabedoria;
    [Range(0f, 10f)] public float inteligencia;
    [Range(0f, 10f)] public float vitalidade;
    [Range(0f, 10f)] public float destreza;
}

public enum TipoElemento
{
    Fisico,
    Fogo,      
    Gelo,      
    Eletrico,  
    Veneno     
}

public enum RaridadeItem
{
    Comum,
    Incomum,
    Raro,
    Epico,     
    Lendario   
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
    AumentarDano,
    Corrosao,    
    RoubarVida,  
    Invulneravel,
    AumentarDuracaoDebuffs // NOVO
}

public enum ModoEfeito { Ativo, Passivo }
public enum AlvoEfeito { Oponente, Usuario, Adjacentes }

[System.Serializable]
public class ComponenteDano
{
    public TipoElemento tipo;
    public int danoBase;
    [Tooltip("Multiplicador do atributo (Ex: 1 = 1x o atributo).")]
    public PorcentagemAtributos escalaAtributos;
}

[System.Serializable]
public class EfeitoItemConfig
{
    public TipoEfeitoItem tipoEfeito;
    public ModoEfeito modo;
    public AlvoEfeito alvo;
    
    [Tooltip("Duração em segundos. Se for 0 e o modo for Ativo, o efeito acumula permanentemente no combate.")]
    public float duracao; 

    [Tooltip("Valor da magnitude (Dano extra, Cura, TEMPO ADICIONAL, etc).")]
    public float valor;
    
    public TipoElemento elementoAlvo; 
    [Tooltip("Soma dos valores dos Tipos (Arma=1, Utensilio=2...). Para UsarAdjacente: 0=Todos, 1=Um.")]
    public int parametroExtra; 
}

// --- CLASSE PRINCIPAL ---

[CreateAssetMenu(menuName = "Item/ItemCombate")]
public class ItemCombate : ScriptableObject
{
    [Header("Identificação")]
    public LocalizedString nomeItem; // Alterado para tradução
    public RaridadeItem raridade;
    public Sprite iconeItem;
    public LocalizedString descricaoItem; // Alterado para tradução

    [Header("Economia")]
    [Min(0)] public int valorOuro; 

    [Header("Características Físicas")]
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

    public bool PossuiTipo(int maskFiltro)
    {
        if (maskFiltro == 0) return true;
        if (tipos == null) return false;
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
            heroi.forca * escala.forca +
            heroi.carisma * escala.carisma +
            heroi.sabedoria * escala.sabedoria +
            heroi.inteligencia * escala.inteligencia +
            heroi.vitalidade * escala.vitalidade +
            heroi.destreza * escala.destreza
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
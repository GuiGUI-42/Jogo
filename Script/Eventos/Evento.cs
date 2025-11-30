using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum EventoLocal
{
    // Renomeei 'Cidade' para 'CentroDaCidade' mantendo o índice 0 para não quebrar assets antigos
    CentroDaCidade = 0, 
    Floresta = 1,
    
    // Novos Locais
    Ruas = 2,
    ZonaComercial = 3,
    Cemiterio = 4,
    Igreja = 5,
    Comunidade = 6,
    VilarejosFloresta = 7,
    VilarejosMontanha = 8,
    Metro = 9,
    Rio = 10,
    Porto = 11
}

// Enum para a caixa de seleção de Slots (1 a 4)
public enum EventoSlots
{
    Um = 1,
    Dois = 2,
    Tres = 3,
    Quatro = 4
}
public enum CategoriaEvento
{
    Normal,
    Aventura
}

[System.Serializable]
public class EventoOpcao
{
    public string nomeOpcao;
    public string descricao;
    public Sprite icone;
    [Tooltip("Quando marcado, usa o campo 'icone' para sobrescrever o sprite do prefab do botão. Quando desmarcado, usa o sprite do prefab.")]
    public bool usarIconeDaOpcao = false;
    [Tooltip("Tipo desta opção (Combate ou Passivo).")]
    public TipoEvento tipo = TipoEvento.Combate;
    [Tooltip("Modificadores aplicados se tipo=Passivo.")]
    public List<PassivoModificador> efeitosPassivos = new List<PassivoModificador>();
    [Tooltip("Lista de possíveis drops ao escolher esta opção.")]
    public List<ItemDrop> drops = new List<ItemDrop>();
}

[CreateAssetMenu(menuName = "Evento/Evento")]
public class Evento : ScriptableObject
{
    public string nomeEvento;
    public Sprite iconeEvento;
    [TextArea] public string descricaoEvento;
    
    [Header("Contexto e Regras")]
    [Tooltip("Local onde este evento pode ocorrer.")]
    public EventoLocal local = EventoLocal.CentroDaCidade;

    [Tooltip("Número de slots que este evento ocupa.")]
    public EventoSlots slotsNecessarios = EventoSlots.Um;

    [Header("Disponibilidade (Semanas)")]
    [Tooltip("Semana inicial em que o evento pode aparecer.")]
    [Min(1)] public int semanaMin = 1;
    [Tooltip("Semana final em que o evento pode aparecer.")]
    [Min(1)] public int semanaMax = 1;

    [Header("Recompensas do Evento")]
    public int recompensaReputacao;
    public int recompensaOuro;
    
    [Header("Categoria do Evento")]
    public CategoriaEvento categoria = CategoriaEvento.Normal;

    [Tooltip("Se for Aventura, este evento só aparecerá se o Antecessor tiver sido completado com sucesso.")]
    public Evento antecessor;
    // Antigo (oculto)
    [HideInInspector] public Heroi monstro; 

    // Preferido: arraste aqui o PREFAB do monstro (do Project)
    [Header("Inimigo")]
    public GameObject monstroPrefab;

    [Header("Opções de Decisão")]
    [Tooltip("Lista de opções apresentadas ao jogador: cada opção tem nome, descrição, ícone e tipo (Combate ou Passivo).")]
    public EventoOpcao[] opcoesDecisao;

    // Helpers para UI
    public int QuantidadeOpcoes => opcoesDecisao == null ? 0 : opcoesDecisao.Length;
    public EventoOpcao ObterOpcao(int indice)
    {
        if (opcoesDecisao == null || indice < 0 || indice >= opcoesDecisao.Length) return null;
        return opcoesDecisao[indice];
    }

    // Validação simples para garantir que min não seja maior que max
    void OnValidate()
    {
        if (semanaMax < semanaMin) semanaMax = semanaMin;
    }
}

// Tipo de uma opção (não do evento inteiro)
public enum TipoEvento { Combate = 0, Passivo = 1 }

// Tipos de atributos possíveis para modificadores passivos
public enum TipoAtributo
{
    Forca,
    Carisma,
    Sabedoria,
    Inteligencia,
    Vitalidade,
    Destreza
}

[System.Serializable]
public class PassivoModificador
{
    public TipoAtributo atributo;
    public int valor; 
}

[System.Serializable]
public class ItemDrop
{
    [Tooltip("Asset do item que pode cair.")]
    public ScriptableObject item;
    [Min(1)] public int quantidadeMin = 1;
    [Min(1)] public int quantidadeMax = 1;
    [Range(0f,1f)] public float chance = 1f; 
}
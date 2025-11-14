using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum EventoLocal
{
    Cidade = 0,
    Floresta = 1,
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
    [Header("Contexto")]
    [Tooltip("Local onde este evento pode ocorrer (ex.: Cidade, Floresta)")]
    public EventoLocal local = EventoLocal.Cidade;

    // Antigo (opcional, pode deixar vazio)
    public Heroi monstro;

    // Preferido: arraste aqui o PREFAB do monstro (do Project)
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
    public int valor; // Valor absoluto ou incremento – interpretar conforme sua lógica futura
}

[System.Serializable]
public class ItemDrop
{
    [Tooltip("Asset do item que pode cair.")]
    public ScriptableObject item;
    [Min(1)] public int quantidadeMin = 1;
    [Min(1)] public int quantidadeMax = 1;
    [Range(0f,1f)] public float chance = 1f; // 1 = sempre, 0.5 = 50%
}
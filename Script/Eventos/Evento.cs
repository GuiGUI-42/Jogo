using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization; // Necessário para tradução

[System.Serializable]
public enum EventoLocal
{
    CentroDaCidade = 0, 
    Floresta = 1,
    Ruas = 2,
    ZonaComercial = 3,
    Cemiterio = 4,
    Igreja = 5,
    Comunidade = 6,
    VilarejosFloresta = 7,
    VilarejosMontanha = 8,
    Metro = 9,
    Rio = 10,
    Porto = 11,
    Montanha = 12
}

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

public enum TipoEvento 
{ 
    Combate = 0, 
    Passivo = 1,
    Loja = 2,
    Ouro = 3
}

[System.Serializable]
public class EventoOpcao
{
    public LocalizedString nomeOpcao; // Alterado para tradução
    public LocalizedString descricao; // Alterado para tradução
    public Sprite icone;
    [Tooltip("Quando marcado, usa o campo 'icone' para sobrescrever o sprite do prefab do botão.")]
    public bool usarIconeDaOpcao = false;
    
    [Tooltip("Tipo desta opção (Combate, Passivo, Loja, Ouro).")]
    public TipoEvento tipo = TipoEvento.Combate;

    [Tooltip("Modificadores aplicados se tipo=Passivo.")]
    public List<PassivoModificador> efeitosPassivos = new List<PassivoModificador>();
    
    // --- NOVO: Lógica de Drop de Personagem ---
    [Header("Drop Especial (Combate)")]
    [Tooltip("Se marcado, existe 20% de chance de dropar o personagem definido ao vencer. Se isso ocorrer, os itens abaixo NÃO dropam.")]
    public bool personagemDropavel;
    
    [Tooltip("O ScriptableObject do Herói/Monstro a ser adicionado ao inventário caso o drop de 20% ocorra.")]
    public ScriptableObject personagemAsset;
    // ------------------------------------------

    [Tooltip("Lista de possíveis drops (ou itens da loja).")]
    public List<ItemDrop> drops = new List<ItemDrop>();

    [Tooltip("Quantidade de Ouro necessária para escolher esta opção (Se tipo=Ouro).")]
    public int custoOuro; 

    /// <summary>
    /// Calcula quais recompensas o jogador receberá.
    /// Regra: 20% de chance de vir o Personagem (se ativado). 
    /// Se falhar (80%), calcula os drops de itens normalmente.
    /// </summary>
    public List<ScriptableObject> ResolverDrops()
    {
        List<ScriptableObject> resultado = new List<ScriptableObject>();

        // 1. Tenta o drop do Personagem (Apenas se for Combate e estiver marcado)
        if (tipo == TipoEvento.Combate && personagemDropavel && personagemAsset != null)
        {
            // Random.value retorna entre 0.0 e 1.0. <= 0.2 é 20%
            if (Random.value <= 0.2f)
            {
                resultado.Add(personagemAsset);
                return resultado; // Retorna apenas o personagem, ignorando itens
            }
        }

        // 2. Se não dropou personagem (ou não estava configurado), processa itens normais
        if (drops != null)
        {
            foreach (var drop in drops)
            {
                if (drop.item == null) continue;

                // Verifica a chance individual de cada item
                if (Random.value <= drop.chance)
                {
                    int qtd = Random.Range(drop.quantidadeMin, drop.quantidadeMax + 1);
                    for (int i = 0; i < qtd; i++)
                    {
                        resultado.Add(drop.item);
                    }
                }
            }
        }

        return resultado;
    }
}

[CreateAssetMenu(menuName = "Evento/Evento")]
public class Evento : ScriptableObject
{
    public LocalizedString nomeEvento; // Alterado para tradução
    public Sprite iconeEvento;
    public LocalizedString descricaoEvento; // Alterado para tradução
    
    [Header("Contexto e Regras")]
    public EventoLocal local = EventoLocal.CentroDaCidade;
    public EventoSlots slotsNecessarios = EventoSlots.Um;

    [Header("Disponibilidade (Semanas)")]
    [Min(1)] public int semanaMin = 1;
    [Min(1)] public int semanaMax = 1;

    [Header("Recompensas do Evento")]
    public int recompensaReputacao;
    public int recompensaOuro;
    
    [Header("Categoria do Evento")]
    public CategoriaEvento categoria = CategoriaEvento.Normal;
    public Evento antecessor;

    [HideInInspector] public Heroi monstro; 

    [Header("Inimigo")]
    public GameObject monstroPrefab;

    [Header("Opções de Decisão")]
    public EventoOpcao[] opcoesDecisao;

    public int QuantidadeOpcoes => opcoesDecisao == null ? 0 : opcoesDecisao.Length;
    public EventoOpcao ObterOpcao(int indice)
    {
        if (opcoesDecisao == null || indice < 0 || indice >= opcoesDecisao.Length) return null;
        return opcoesDecisao[indice];
    }

    void OnValidate()
    {
        if (semanaMax < semanaMin) semanaMax = semanaMin;
    }
}

public enum TipoAtributo
{
    Forca, Carisma, Sabedoria, Inteligencia, Vitalidade, Destreza
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
    public ScriptableObject item;
    [Min(1)] public int quantidadeMin = 1;
    [Min(1)] public int quantidadeMax = 1;
    [Range(0f,1f)] public float chance = 1f; 
}
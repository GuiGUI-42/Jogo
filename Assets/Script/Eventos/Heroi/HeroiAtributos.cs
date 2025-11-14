using UnityEngine;

public class HeroiAtributos : MonoBehaviour, IAtributos
{
    [Header("Base (ScriptableObject)")]
    public Heroi baseAtributos;

    [Header("Atributos Atuais (runtime)")]
    public int forcaAtual;
    public int carismaAtual;
    public int sabedoriaAtual;
    public int inteligenciaAtual;
    public int vitalidadeAtual;
    public int destrezaAtual;

    [Header("Itens Iniciais")]
    public ItemCombate[] itensIniciais;

    [Header("Inventário Dinâmico (slots)")]
    [Tooltip("Slots atuais do inventário do herói (podem incluir itens iniciais).")]
    public ScriptableObject[] slotsInventario = new ScriptableObject[9];

    void Awake()
    {
        ResetarParaBase();
        InicializarInventario();
    }

    public void ResetarParaBase()
    {
        if (baseAtributos == null) return;
        forcaAtual       = baseAtributos.forca;
        carismaAtual     = baseAtributos.carisma;
        sabedoriaAtual   = baseAtributos.sabedoria;
        inteligenciaAtual= baseAtributos.inteligencia;
        vitalidadeAtual  = baseAtributos.vitalidade;
        destrezaAtual    = baseAtributos.destreza;
    }

    void InicializarInventario()
    {
        if (slotsInventario == null || slotsInventario.Length == 0)
            slotsInventario = new ScriptableObject[9];
        if (itensIniciais != null)
        {
            for (int i = 0; i < itensIniciais.Length && i < slotsInventario.Length; i++)
            {
                slotsInventario[i] = itensIniciais[i];
            }
        }
    }

    public bool TentarAdicionarAsset(ScriptableObject asset)
    {
        if (!asset) return false;
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            if (slotsInventario[i] == null)
            {
                slotsInventario[i] = asset;
                return true;
            }
        }
        return false; // sem espaço
    }

    // Implementação da interface IAtributos
    public int forca => forcaAtual;
    public int carisma => carismaAtual;
    public int sabedoria => sabedoriaAtual;
    public int inteligencia => inteligenciaAtual;
    public int vitalidade => vitalidadeAtual;
    public int destreza => destrezaAtual;
}
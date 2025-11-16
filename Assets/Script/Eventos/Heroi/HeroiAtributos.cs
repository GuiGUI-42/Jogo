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

    [Header("Inventário Dinâmico (slots)")]
    [Tooltip("Slots atuais do inventário do herói (podem incluir itens iniciais).")]
    public ScriptableObject[] slotsInventario = new ScriptableObject[9];

    // Evento disparado sempre que o inventário (slotsInventario) é alterado
    public System.Action<HeroiAtributos> OnInventarioAlterado;

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
    }

    public bool TentarAdicionarAsset(ScriptableObject asset)
    {
        if (!asset) return false;
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            if (slotsInventario[i] == null)
            {
                slotsInventario[i] = asset;
                Debug.Log("[HeroiAtributos] Adicionado asset '" + asset.name + "' no slot " + i + " do herói " + name);
                OnInventarioAlterado?.Invoke(this);
                return true;
            }
        }
        return false; // sem espaço
    }

    public bool RemoverAssetNoIndice(int index)
    {
        if (index < 0 || index >= slotsInventario.Length) return false;
        if (slotsInventario[index] == null) return false;
        Debug.Log("[HeroiAtributos] Removendo asset '" + slotsInventario[index].name + "' do slot " + index + " (herói " + name + ")");
        slotsInventario[index] = null;
        OnInventarioAlterado?.Invoke(this);
        return true;
    }

    public bool RemoverAsset(ScriptableObject asset)
    {
        if (!asset) return false;
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            if (slotsInventario[i] == asset)
            {
                slotsInventario[i] = null;
                Debug.Log("[HeroiAtributos] Removendo asset '" + asset.name + "' (busca por referência) do herói " + name);
                OnInventarioAlterado?.Invoke(this);
                return true;
            }
        }
        return false;
    }

    // Implementação da interface IAtributos
    public int forca => forcaAtual;
    public int carisma => carismaAtual;
    public int sabedoria => sabedoriaAtual;
    public int inteligencia => inteligenciaAtual;
    public int vitalidade => vitalidadeAtual;
    public int destreza => destrezaAtual;
}
using UnityEngine;

public enum TipoDano
{
    Fisico,
    Elemental
}

public enum Elemento
{
    Nenhum,
    Fogo,
    Agua,
    Terra,
    Ar
}

[System.Serializable]
public class PorcentagemAtributos
{
    [Range(0, 100)] public float forca;
    [Range(0, 100)] public float carisma;
    [Range(0, 100)] public float sabedoria;
    [Range(0, 100)] public float inteligencia;
    [Range(0, 100)] public float vitalidade;
    [Range(0, 100)] public float destreza;
}

[CreateAssetMenu(menuName = "Item/ItemCombate")]
public class ItemCombate : ScriptableObject
{
    public string nomeItem;
    public Sprite iconeItem;
    public string descricaoItem;

    public TipoDano tipoDano;
    public Elemento elemento = Elemento.Nenhum;

    public PorcentagemAtributos porcentagemFisico;
    public PorcentagemAtributos porcentagemElemental;

    public int danoBaseFisico;
    public int danoBaseElemental;

    // Tempo de recarga entre ativações em combate (segundos)
    [Min(0f)] public float cooldownSegundos = 1f;

    // Cura: base + percentual da Vitalidade do usuário
    [Min(0)] public int curaBase;
    [Range(0, 500)] public float curaPercentualVitalidade;

    // Exemplo de cálculo de dano
    public int CalcularDanoFisico(Heroi heroi)
    {
        return danoBaseFisico +
            Mathf.RoundToInt(
                heroi.forca * (porcentagemFisico.forca / 100f) +
                heroi.carisma * (porcentagemFisico.carisma / 100f) +
                heroi.sabedoria * (porcentagemFisico.sabedoria / 100f) +
                heroi.inteligencia * (porcentagemFisico.inteligencia / 100f) +
                heroi.vitalidade * (porcentagemFisico.vitalidade / 100f) +
                heroi.destreza * (porcentagemFisico.destreza / 100f)
            );
    }

    public int CalcularDanoElemental(Heroi heroi)
    {
        return danoBaseElemental +
            Mathf.RoundToInt(
                heroi.forca * (porcentagemElemental.forca / 100f) +
                heroi.carisma * (porcentagemElemental.carisma / 100f) +
                heroi.sabedoria * (porcentagemElemental.sabedoria / 100f) +
                heroi.inteligencia * (porcentagemElemental.inteligencia / 100f) +
                heroi.vitalidade * (porcentagemElemental.vitalidade / 100f) +
                heroi.destreza * (porcentagemElemental.destreza / 100f)
            );
    }

    // Cura total: base + percentual da Vitalidade
    public int CalcularCura(Heroi heroi)
    {
        if (heroi == null) return 0;
        int bonus = Mathf.RoundToInt(heroi.vitalidade * (curaPercentualVitalidade / 100f));
        int total = curaBase + bonus;
        return total > 0 ? total : 0;
    }

    // Helpers de cooldown (use Time.time como referência de tempo)
    public bool PodeAtivar(float ultimoUsoTime)
    {
        return (Time.time - ultimoUsoTime) >= cooldownSegundos;
    }

    public float TempoRestante(float ultimoUsoTime)
    {
        float restante = cooldownSegundos - (Time.time - ultimoUsoTime);
        return restante > 0f ? restante : 0f;
    }

    public float ProximoDisponivelEm(float ultimoUsoTime)
    {
        return ultimoUsoTime + cooldownSegundos;
    }
}
using UnityEngine;
using UnityEngine.Localization; // Necessário para tradução

[CreateAssetMenu(menuName = "Heroi/HeroiBase")]
public class Heroi : ScriptableObject
{
    public LocalizedString nomeHeroi; // Alterado para tradução
    public Sprite iconeHeroi;
    public LocalizedString descricaoHeroi; // Alterado para tradução

    [Header("Atributos")]
    public int forca;
    public int carisma;
    public int sabedoria;
    public int inteligencia;
    public int vitalidade;
    public int destreza;
}
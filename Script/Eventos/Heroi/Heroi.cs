using UnityEngine;

[CreateAssetMenu(menuName = "Heroi/HeroiBase")]
public class Heroi : ScriptableObject
{
    public string nomeHeroi;
    public Sprite iconeHeroi;
    public string descricaoHeroi;

    [Header("Atributos")]
    public int forca;
    public int carisma;
    public int sabedoria;
    public int inteligencia;
    public int vitalidade;
    public int destreza;
}
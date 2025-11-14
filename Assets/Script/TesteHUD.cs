using UnityEngine;

public class TesteHUD : MonoBehaviour
{
    public HUDPersonagens hudPersonagens; // arraste o objeto HUDPersonagens no Inspector
    public GameObject[] novosHerois;      // arraste os prefabs dos heróis iniciais no Inspector

    void Start()
    {
        hudPersonagens.AtualizarHUD(novosHerois);
    }
}
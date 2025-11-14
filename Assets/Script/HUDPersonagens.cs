using UnityEngine;

public class HUDPersonagens : MonoBehaviour
{
    public GameObject[] heroiPrefabs; // Prefabs dos heróis
    public GameObject inventarioUI;
    
    void Start()
    {
        MostrarHerois();
    }

    void MostrarHerois()
    {
        // Remove heróis antigos (se houver)
        foreach (Transform filho in transform)
        {
            Destroy(filho.gameObject);
        }

        // Instancia os prefabs dos heróis como filhos deste objeto
        if (heroiPrefabs != null)
        {
            foreach (var prefab in heroiPrefabs)
            {
                if (prefab)
                {
                    Instantiate(prefab, transform);
                }
            }
        }
    }

    public void AtualizarHUD(GameObject[] novosHerois)
    {
        heroiPrefabs = novosHerois;
        MostrarHerois();
    }
    public void AbrirInventarioHeroi(HeroiSelecionavel selecionavel)
    {
        var atributos = selecionavel.GetComponent<HeroiAtributos>();
        var inventario = inventarioUI.GetComponentInChildren<InventarioHeroiUI>();
        if (atributos != null && inventario != null)
        {
            inventario.AbrirInventario(atributos);
            inventarioUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Atributos do herói ou InventarioHeroiUI não encontrados!");
        }
    }
}
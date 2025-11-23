using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Collections.Generic;

public class EventoCombateUI : MonoBehaviour
{
    [Header("Referências Gerais")]
    public GameObject painelCombate;
    public CombateSistema sistemaLogico;

    [Header("UI de Resultado")]
    public GameObject uiVitoria;       
    public GameObject uiDerrota;       
    
    [Header("UI de Recompensa (Drop)")]
    public Image imgItemDropado;       
    public GameObject painelDrop;      

    [Header("Containers (Molduras)")] 
    public GameObject containerSlotHeroi; 
    public GameObject containerSlotMonstro;
    public GameObject containerNome; 

    [Header("Heroi UI")]
    public Image imgHeroi;
    public TextMeshProUGUI txtNomeHeroi;
    public HealthBar barraVidaHeroi;
    public Transform slotItemsHeroi;

    [Header("Monstro UI")]
    public Image imgMonstro;
    public TextMeshProUGUI txtNomeMonstro;
    public HealthBar barraVidaMonstro;
    public Transform slotItemsMonstro;

    private GameObject monstroInstancia;
    private EventoOpcao opcaoOrigem; 

    void Start()
    {
        if(painelCombate) painelCombate.SetActive(false);
        if(uiVitoria) uiVitoria.SetActive(false);
        if(uiDerrota) uiDerrota.SetActive(false);
        if(painelDrop) painelDrop.SetActive(false);

        if (sistemaLogico)
        {
            sistemaLogico.OnVidaAtualizada += AtualizarBarras;
            sistemaLogico.OnCombateFinalizado += FinalizarCombateUI;
        }
    }

    public void IniciarCombate(HeroiAtributos heroiSelecionado, GameObject prefabMonstro, EventoOpcao dadosOpcao)
    {
        this.opcaoOrigem = dadosOpcao;

        if (heroiSelecionado == null || prefabMonstro == null)
        {
            Debug.LogError("[EventoCombateUI] Faltando herói ou monstro!");
            return;
        }

        painelCombate.SetActive(true);
        SetElementosCombateAtivos(true); 

        if(uiVitoria) uiVitoria.SetActive(false);
        if(uiDerrota) uiDerrota.SetActive(false);
        if(painelDrop) painelDrop.SetActive(false);

        // Configura Visual do Herói
        if (heroiSelecionado.baseAtributos != null)
        {
            if(imgHeroi) {
                imgHeroi.sprite = heroiSelecionado.baseAtributos.iconeHeroi;
                imgHeroi.preserveAspect = true;
                imgHeroi.color = Color.white;
            }
            if(txtNomeHeroi) txtNomeHeroi.text = heroiSelecionado.baseAtributos.nomeHeroi;
            AtualizarIconesItens(heroiSelecionado, slotItemsHeroi);
        }

        // Configura Visual do Monstro
        if (monstroInstancia != null) Destroy(monstroInstancia);
        monstroInstancia = Instantiate(prefabMonstro, transform); 
        monstroInstancia.SetActive(false); 

        var atributosMonstro = monstroInstancia.GetComponent<HeroiAtributos>();
        if (atributosMonstro && atributosMonstro.baseAtributos)
        {
            if(imgMonstro) {
                imgMonstro.sprite = atributosMonstro.baseAtributos.iconeHeroi;
                imgMonstro.preserveAspect = true;
                imgMonstro.color = Color.white;
            }
            if(txtNomeMonstro) txtNomeMonstro.text = atributosMonstro.baseAtributos.nomeHeroi;
            AtualizarIconesItens(atributosMonstro, slotItemsMonstro);
        }
        else
        {
            if(txtNomeMonstro) txtNomeMonstro.text = prefabMonstro.name;
            var spriteR = prefabMonstro.GetComponent<SpriteRenderer>();
            if(spriteR && imgMonstro) 
            {
                imgMonstro.sprite = spriteR.sprite;
                imgMonstro.color = Color.white;
            }
            LimparSlots(slotItemsMonstro);
        }

        sistemaLogico.Iniciar(heroiSelecionado.gameObject, monstroInstancia);
    }

    void FinalizarCombateUI(ResultadoCombate resultado)
    {
        Debug.Log($"Combate terminou! Resultado: {resultado}");
        
        if (resultado == ResultadoCombate.HeroiVenceu)
        {
            if(uiVitoria) uiVitoria.SetActive(true);
            Invoke(nameof(MostrarDrops), 2f);
        }
        else
        {
            if(uiDerrota) uiDerrota.SetActive(true);
            Invoke(nameof(FecharCombate), 2f);
        }
    }

    void MostrarDrops()
    {
        if(uiVitoria) uiVitoria.SetActive(false);
        SetElementosCombateAtivos(false);

        ScriptableObject itemDropado = CalcularDrop();

        if (itemDropado != null)
        {
            if(painelDrop) painelDrop.SetActive(true);
            if(imgItemDropado)
            {
                imgItemDropado.sprite = ExtrairSprite(itemDropado);
                imgItemDropado.preserveAspect = true;
                imgItemDropado.color = Color.white;

                var dragComp = imgItemDropado.GetComponent<DraggableDropItem>();
                if (dragComp == null) dragComp = imgItemDropado.gameObject.AddComponent<DraggableDropItem>();

                dragComp.asset = itemDropado;
                dragComp.quantidade = 1;
                dragComp.sourceImage = imgItemDropado;

                dragComp.OnItemArrastadoComSucesso -= OnDropRealizado; 
                dragComp.OnItemArrastadoComSucesso += OnDropRealizado; 

                Debug.Log("Aguardando jogador arrastar o item...");
            }
        }
        else
        {
            Debug.Log("Nenhum item dropado.");
            FecharCombate();
        }
    }

    void OnDropRealizado()
    {
        Debug.Log("Item coletado! Fechando interface.");
        if (imgItemDropado)
        {
            var dragComp = imgItemDropado.GetComponent<DraggableDropItem>();
            if(dragComp) dragComp.OnItemArrastadoComSucesso -= OnDropRealizado;
        }
        FecharCombate();
    }

    void SetElementosCombateAtivos(bool ativo)
    {
        if (containerSlotHeroi) containerSlotHeroi.SetActive(ativo);
        if (containerSlotMonstro) containerSlotMonstro.SetActive(ativo);
        if (containerNome) containerNome.SetActive(ativo);

        if (barraVidaHeroi) barraVidaHeroi.gameObject.SetActive(ativo);
        if (barraVidaMonstro) barraVidaMonstro.gameObject.SetActive(ativo);
        
        if (imgHeroi && (!containerSlotHeroi || !imgHeroi.transform.IsChildOf(containerSlotHeroi.transform))) 
            imgHeroi.gameObject.SetActive(ativo);
    }

    ScriptableObject CalcularDrop()
    {
        if (opcaoOrigem == null || opcaoOrigem.drops == null) return null;

        foreach (var dropInfo in opcaoOrigem.drops)
        {
            float rolagem = Random.value;
            if (rolagem <= dropInfo.chance) return dropInfo.item; 
        }
        return null;
    }

    void FecharCombate()
    {
        painelCombate.SetActive(false);
        if(uiDerrota) uiDerrota.SetActive(false);
        if(painelDrop) painelDrop.SetActive(false);
        
        if (monstroInstancia != null) Destroy(monstroInstancia);
        
        // --- CORREÇÃO AQUI: FindObjectsInactive.Include ---
        EventoUI eventoUI = FindFirstObjectByType<EventoUI>(FindObjectsInactive.Include);
        
        if (eventoUI != null)
        {
             eventoUI.FinalizarCicloDoEvento();
        }
        else
        {
             // Fallback
             eventoUI = FindAnyObjectByType<EventoUI>(FindObjectsInactive.Include);
             if(eventoUI) eventoUI.FinalizarCicloDoEvento();
             else Debug.LogWarning("[EventoCombateUI] Não encontrou EventoUI para finalizar ciclo.");
        }
    }

    void AtualizarIconesItens(HeroiAtributos atributos, Transform container)
    {
        if (container == null) return;
        LimparSlots(container);
        if (atributos.slotsInventario != null)
        {
            foreach (var item in atributos.slotsInventario)
            {
                if (item == null) continue;
                GameObject iconeObj = new GameObject("IconeItem", typeof(RectTransform), typeof(Image));
                iconeObj.transform.SetParent(container, false);
                Image img = iconeObj.GetComponent<Image>();
                img.sprite = ExtrairSprite(item);
                img.preserveAspect = true;
            }
        }
    }

    void LimparSlots(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
    }

    void AtualizarBarras(float vidaHeroi, float maxHeroi, float vidaMonstro, float maxMonstro)
    {
        if (barraVidaHeroi) barraVidaHeroi.SetValores(vidaHeroi, maxHeroi);
        if (barraVidaMonstro) barraVidaMonstro.SetValores(vidaMonstro, maxMonstro);
    }

    Sprite ExtrairSprite(ScriptableObject asset)
    {
        if (!asset) return null;
        var t = asset.GetType();
        var f = t.GetField("iconeItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        f = t.GetField("icone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        return null;
    }
}
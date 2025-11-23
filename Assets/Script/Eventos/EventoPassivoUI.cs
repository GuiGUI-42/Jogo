using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EventoPassivoUI : MonoBehaviour
{
    [Header("UI Principal")]
    public GameObject painelPassivo;
    public TextMeshProUGUI textoCalculo;
    
    [Header("UI de Resultado")]
    public GameObject uiVitoria;       
    public GameObject uiDerrota;       

    [Header("UI de Recompensa (Drop)")]
    public Image imgItemDropado;       
    public GameObject painelDrop;      

    private EventoOpcao opcaoAtual;
    private HeroiAtributos heroiAtual;

    void Start()
    {
        if(painelPassivo) painelPassivo.SetActive(false);
        if(uiVitoria) uiVitoria.SetActive(false);
        if(uiDerrota) uiDerrota.SetActive(false);
        if(painelDrop) painelDrop.SetActive(false);
    }

    public void ResolverPassivo(HeroiAtributos heroi, EventoOpcao opcao)
    {
        this.heroiAtual = heroi;
        this.opcaoAtual = opcao;

        painelPassivo.SetActive(true);
        
        if(uiVitoria) uiVitoria.SetActive(false);
        if(uiDerrota) uiDerrota.SetActive(false);
        if(painelDrop) painelDrop.SetActive(false);
        if(imgItemDropado) imgItemDropado.gameObject.SetActive(false);

        StartCoroutine(RotinaCalculo());
    }

    IEnumerator RotinaCalculo()
    {
        int dificuldadeTotal = 0;
        int bonusHeroi = 0;

        foreach (var efeito in opcaoAtual.efeitosPassivos)
        {
            dificuldadeTotal += efeito.valor;
            bonusHeroi += heroiAtual.GetValorAtributo(efeito.atributo);
        }

        int d20 = Random.Range(1, 21);
        int resultadoFinal = d20 + bonusHeroi;
        bool sucesso = resultadoFinal >= dificuldadeTotal;

        if(textoCalculo)
        {
            textoCalculo.text = $"Rolando dado...";
            yield return new WaitForSeconds(1f);
            textoCalculo.text = $"D20 ({d20}) + Atributos ({bonusHeroi}) = <size=120%>{resultadoFinal}</size>\nCD: {dificuldadeTotal}";
        }

        yield return new WaitForSeconds(2f);

        if (sucesso)
        {
            if(uiVitoria) uiVitoria.SetActive(true);
            yield return new WaitForSeconds(2f);
            MostrarDrops();
        }
        else
        {
            if(uiDerrota) uiDerrota.SetActive(true);
            yield return new WaitForSeconds(2f);
            FecharTudo();
        }
    }

    void MostrarDrops()
    {
        if(uiVitoria) uiVitoria.SetActive(false);
        if(textoCalculo) textoCalculo.gameObject.SetActive(false);

        ScriptableObject itemDropado = CalcularDrop();

        if (itemDropado != null)
        {
            if(painelDrop) painelDrop.SetActive(true);
            if(imgItemDropado)
            {
                imgItemDropado.gameObject.SetActive(true);
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
            }
        }
        else
        {
            FecharTudo();
        }
    }

    ScriptableObject CalcularDrop()
    {
        if (opcaoAtual == null || opcaoAtual.drops == null) return null;
        foreach (var dropInfo in opcaoAtual.drops)
        {
            float rolagem = Random.value;
            if (rolagem <= dropInfo.chance) return dropInfo.item;
        }
        return null;
    }

    void OnDropRealizado()
    {
        if (imgItemDropado)
        {
            var dragComp = imgItemDropado.GetComponent<DraggableDropItem>();
            if(dragComp) dragComp.OnItemArrastadoComSucesso -= OnDropRealizado;
        }
        FecharTudo();
    }

    void FecharTudo()
    {
        painelPassivo.SetActive(false);
        
        // --- CORREÇÃO AQUI: FindObjectsInactive.Include ---
        // Isso garante encontrar o EventoUI mesmo se o GameObject dele estiver desativado
        EventoUI eventoUI = FindFirstObjectByType<EventoUI>(FindObjectsInactive.Include);
        
        if (eventoUI != null)
        {
            eventoUI.FinalizarCicloDoEvento();
        }
        else
        {
            // Tenta achar qualquer objeto se a versão do Unity for antiga e não suportar o parametro acima em FindFirst
            eventoUI = FindAnyObjectByType<EventoUI>(FindObjectsInactive.Include);
            if(eventoUI != null) eventoUI.FinalizarCicloDoEvento();
            else Debug.LogError("[EventoPassivoUI] CRÍTICO: Não encontrou EventoUI para finalizar o ciclo!");
        }
    }

    Sprite ExtrairSprite(ScriptableObject asset)
    {
        if (!asset) return null;
        var t = asset.GetType();
        var f = t.GetField("iconeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        f = t.GetField("icone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(asset) as Sprite;
        return null;
    }
}
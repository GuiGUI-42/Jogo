using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventoLojaUI : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject painelLoja;
    public Transform containerSlotsLoja;
    public GameObject prefabSlotLoja;
    public TextMeshProUGUI txtOuroLoja;

    // Não precisamos mais de precoMinimo e precoMaximo aqui!

    private EventoUI eventoUIPai;

    void Start()
    {
        if(painelLoja) painelLoja.SetActive(false);
    }

    void Update()
    {
        if (painelLoja.activeSelf && GameManager.instance && txtOuroLoja)
        {
            txtOuroLoja.text = $"Seu Ouro: {GameManager.instance.ouroAtual}";
        }
    }

    public void AbrirLoja(EventoOpcao opcao, EventoUI uiPai)
    {
        this.eventoUIPai = uiPai;
        painelLoja.SetActive(true);

        foreach (Transform child in containerSlotsLoja) Destroy(child.gameObject);

        if (opcao.drops == null) return;

        foreach (var drop in opcao.drops)
        {
            if (drop.item == null) continue;
            CriarItemNaLoja(drop.item);
        }
    }

    void CriarItemNaLoja(ScriptableObject itemAsset)
    {
        GameObject slotObj = Instantiate(prefabSlotLoja, containerSlotsLoja);
        
        Image img = slotObj.GetComponent<Image>();
        if (img == null) img = slotObj.AddComponent<Image>();
        img.sprite = ExtrairSprite(itemAsset);
        img.preserveAspect = true;

        // --- CÁLCULO DINÂMICO DE PREÇO ---
        int precoDesteItem = 0;
        
        // Verifica se é um Item para pegar o preço dinâmico, senão usa um padrão
        if (itemAsset is Item itemData)
        {
            precoDesteItem = itemData.GetValorCompra();
        }
        else
        {
            precoDesteItem = 50; // Valor fallback para assets antigos sem classe Item
        }
        // ---------------------------------

        CriarTextoPreco(slotObj, precoDesteItem);

        var drag = slotObj.AddComponent<DraggableDropItem>();
        drag.item = itemAsset as Item;
        drag.asset = itemAsset;
        drag.sourceImage = img;
        drag.quantidade = 1;

        // Condição de Compra
        drag.VerificarCondicaoDeArraste = () => 
        {
            if (GameManager.instance.TemOuroSuficiente(precoDesteItem)) return true;
            else
            {
                Debug.Log("Ouro insuficiente!");
                return false; 
            }
        };

        // Efetuar Compra (Só desconta o ouro, o DragDropTarget destino adiciona o item)
        drag.OnItemArrastadoComSucesso += () => 
        {
            GameManager.instance.GastarOuro(precoDesteItem);
        };
    }

    void CriarTextoPreco(GameObject pai, int valor)
    {
        GameObject txtObj = new GameObject("PrecoTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(pai.transform, false);
        
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0.3f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = $"${valor}"; // Sifrão para indicar compra
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.textWrappingMode = TextWrappingModes.NoWrap; 
    }

    public void BotaoFecharLoja()
    {
        painelLoja.SetActive(false);
        if(eventoUIPai) eventoUIPai.FinalizarCicloDoEvento(true);
    }

    Sprite ExtrairSprite(ScriptableObject asset)
    {
        if (asset is Item i) return i.icon;
        // Fallback para reflection caso use assets antigos
        var t = asset.GetType();
        var f = t.GetField("iconeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.GetValue(asset) is Sprite s1) return s1;
        return null;
    }
}
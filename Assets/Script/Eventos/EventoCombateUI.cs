using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class EventoCombateUI : MonoBehaviour
{
    public static EventoCombateUI Instance;

    public GameObject painelCombate;     // painel do combate (apenas esta janela)
    [Header("Resultado")]
    public GameObject painelVitoria;     // painel a ser mostrado quando herói vencer
    public GameObject painelDerrota;     // painel a ser mostrado quando herói perder
    [Header("Drop de Item")]
    public GameObject dropItemRoot;      // GameObject que mostra o item dropado (ex.: Image)
    public Image dropItemImage;          // Image dentro de dropItemRoot
    // Monstro
    public TMP_Text nomeMonstroText;
    public Image monstroImage;
    public Transform slotItensContainer; // Container de itens do monstro (SlotMonstro/Monstro/Slot_Items)
    [Header("Vida")]
    [Tooltip("Barra de vida do monstro (opcional)")] public HealthBar barraVidaMonstro;

    // Herói
    public TMP_Text nomeHeroiText;
    public Image heroiImage;
    public Transform slotItensHeroiContainer; // Container de itens do herói (SlotHeroi/Slot_Items)
    [Tooltip("Barra de vida do herói (opcional)")] public HealthBar barraVidaHeroi;
    [Tooltip("Opcional: arraste um prefab de herói para testes quando não houver herói selecionado.")]
    public GameObject heroiPrefabPadrao;

    readonly List<Image> itemSlots = new(); // slots do monstro
    readonly List<Image> itemSlotsHeroi = new(); // slots do herói

    // Integração com o sistema de combate
    CombateSistema combateSistema;
    GameObject heroiSelecionadoGO;
    GameObject monstroSelecionadoPrefab;
    EventoOpcao opcaoAtual; // opção de evento usada neste combate (para obter drops)
    ScriptableObject dropAssetAtual; // pode ser Item, ItemCombate, etc.
    Item dropItemAtual; // cache se for Item
    int dropQuantidadeAtual = 1;

    void Awake()
    {
        Instance = this;

        if (!painelCombate) painelCombate = gameObject;
        if (!monstroImage)
            monstroImage = transform.Find("SlotMonstro/Monstro")?.GetComponent<Image>();
        if (!slotItensContainer)
            slotItensContainer = transform.Find("SlotMonstro/Monstro/Slot_Items");
        if (!nomeMonstroText)
            nomeMonstroText = transform.Find("Nome/Nome_Monstro")?.GetComponent<TMP_Text>();

        // Heroi: busca automática se não estiver ligado no Inspector
        if (!heroiImage)
            heroiImage = transform.Find("SlotHeroi/Heroi")?.GetComponent<Image>();
        if (!slotItensHeroiContainer)
            slotItensHeroiContainer = transform.Find("SlotHeroi/Slot_Items");
        if (!nomeHeroiText)
        {
            // Tenta achar um texto de nome do herói; caso não exista, usa o do monstro como fallback
            var t1 = transform.Find("Nome/Nome_Heroi")?.GetComponent<TMP_Text>();
            nomeHeroiText = t1 ? t1 : nomeMonstroText;
        }

        // Localiza UI de drop automaticamente, se não ligado
        if (!dropItemRoot)
        {
            var t = transform.Find("DropItem");
            if (t) dropItemRoot = t.gameObject;
        }
        if (dropItemRoot && !dropItemImage)
        {
            dropItemImage = dropItemRoot.GetComponentInChildren<Image>(true);
        }

        itemSlots.Clear();
        if (slotItensContainer)
        {
            foreach (Transform t in slotItensContainer)
            {
                var img = t.GetComponent<Image>();
                if (img) itemSlots.Add(img);
            }
        }

        if (slotItensHeroiContainer)
        {
            itemSlotsHeroi.Clear();
            foreach (Transform t in slotItensHeroiContainer)
            {
                var img = t.GetComponent<Image>();
                if (img) itemSlotsHeroi.Add(img);
            }
        }

        if (painelCombate) painelCombate.SetActive(false);
        if (painelVitoria) painelVitoria.SetActive(false);
        if (painelDerrota) painelDerrota.SetActive(false);
        if (dropItemRoot) dropItemRoot.SetActive(false);

        combateSistema = GetComponent<CombateSistema>();
        if (!combateSistema) combateSistema = gameObject.AddComponent<CombateSistema>();
        combateSistema.OnVidaAtualizada += AtualizarBarrasDeVida;
        combateSistema.OnCombateFinalizado += OnCombateFinalizado;
    }

    public void DefinirOpcao(EventoOpcao opcao)
    {
        opcaoAtual = opcao;
    }

    // Recebe o PREFAB do monstro (com HeroiAtributos)
    public void AbrirMonstro(GameObject monstroPrefab)
    {
        if (!monstroPrefab)
        {
            Debug.LogError("EventoCombateUI.Abrir: prefab do monstro nulo.");
            return;
        }

        var atributos = monstroPrefab.GetComponent<HeroiAtributos>();
        if (!atributos)
        {
            Debug.LogError("[CombateUI] Prefab do monstro não tem HeroiAtributos.");
            return;
        }

        var baseHeroi = atributos.baseAtributos;
        string nome = "";
        if (baseHeroi != null)
        {
            nome = baseHeroi.nomeHeroi;
            Debug.Log($"[CombateUI] Nome do asset baseAtributos: '{nome}'");
        }
        else
        {
            nome = monstroPrefab.name;
            Debug.Log($"[CombateUI] Nome do prefab: '{nome}'");
        }

        // Ícone: prioriza iconeHeroi; se vazio, pega a primeira Image com sprite no prefab
        Sprite icone = null;
        if (baseHeroi && baseHeroi.iconeHeroi)
        {
            icone = baseHeroi.iconeHeroi;
            Debug.Log("[CombateUI] Usando icone do asset baseAtributos");
        }
        else
        {
            Debug.LogWarning("[CombateUI] O asset do monstro não tem iconeHeroi definido!");
            var imgs = monstroPrefab.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                Debug.Log($"[CombateUI] Encontrou Image: {img.gameObject.name}, sprite={(img.sprite ? img.sprite.name : "null")}");
                if (img.sprite && img.gameObject.name.ToLower().Contains("monstro"))
                {
                    icone = img.sprite;
                    Debug.Log($"[CombateUI] Usando sprite '{icone.name}' do GameObject '{img.gameObject.name}'");
                    break;
                }
            }
        }

        // Itens do componente
        var itensSprites = new List<Sprite>();
        if (atributos.itensIniciais != null)
        {
            Debug.Log($"[CombateUI] itensIniciais count: {atributos.itensIniciais.Length}");
            for (int i = 0; i < atributos.itensIniciais.Length; i++)
            {
                var item = atributos.itensIniciais[i];
                if (!item)
                {
                    Debug.LogWarning($"[CombateUI] Item {i} nulo");
                    continue;
                }
                var spr = ExtrairSpriteDoItem(item);
                Debug.Log($"[CombateUI] Item {i}: {item.name}, sprite={(spr ? spr.name : "null")}");
                if (spr) itensSprites.Add(spr);
            }
        }
        else
        {
            Debug.LogWarning("[CombateUI] Nenhum item inicial encontrado no prefab.");
        }

        if (painelCombate) painelCombate.SetActive(true);
        RestaurarLayoutCombate();

        if (nomeMonstroText)
        {
            nomeMonstroText.gameObject.SetActive(true);
            nomeMonstroText.enabled = true;
            nomeMonstroText.text = string.IsNullOrEmpty(nome) ? "(Sem nome)" : nome;
            var c = nomeMonstroText.color;
            if (c.a < 0.99f) nomeMonstroText.color = new Color(c.r, c.g, c.b, 1f);
            nomeMonstroText.ForceMeshUpdate();
            Debug.Log($"[CombateUI] nomeMonstroText.text='{nomeMonstroText.text}'");
        }
        else
        {
            Debug.LogWarning("[CombateUI] nomeMonstroText não está atribuído!");
        }

        if (monstroImage)
        {
            monstroImage.sprite = icone;
            monstroImage.preserveAspect = true;
            monstroImage.color = icone ? Color.white : new Color(1, 1, 1, 0);
            Debug.Log($"[CombateUI] monstroImage.sprite='{(icone ? icone.name : "null")}'");
        }
        else
        {
            Debug.LogWarning("[CombateUI] monstroImage não está atribuído!");
        }

        PreencherItens(itemSlots, itensSprites);

        Debug.Log($"[CombateUI] (Monstro) nome='{nome}', icone={(icone ? "ok" : "null")}, itens={itensSprites.Count}");

        // Guarda referência para iniciar o combate
        monstroSelecionadoPrefab = monstroPrefab;
        TentarIniciarCombate();
    }

    // Recebe o GameObject do herói (com HeroiAtributos)
    public void AbrirHeroi(GameObject heroiGO)
    {
        if (!heroiGO)
        {
            Debug.LogError("EventoCombateUI.Abrir: heroiGO nulo.");
            return;
        }

        var atributos = heroiGO.GetComponent<HeroiAtributos>();
        if (!atributos)
        {
            Debug.LogError("Prefab do herói não tem HeroiAtributos.");
            return;
        }

        var baseHeroi = atributos.baseAtributos;
        string nome = baseHeroi ? baseHeroi.nomeHeroi : heroiGO.name;
        Sprite icone = null;
        if (baseHeroi && baseHeroi.iconeHeroi)
            icone = baseHeroi.iconeHeroi;

        // Itens do herói
        var itensSprites = new List<Sprite>();
        if (atributos.itensIniciais != null)
        {
            foreach (var item in atributos.itensIniciais)
            {
                if (!item) continue;
                var spr = ExtrairSpriteDoItem(item);
                if (spr) itensSprites.Add(spr);
            }
        }

        // Exibe no painel
        if (painelCombate) painelCombate.SetActive(true);
        RestaurarLayoutCombate();
        if (nomeHeroiText)
        {
            nomeHeroiText.gameObject.SetActive(true);
            nomeHeroiText.enabled = true;
            nomeHeroiText.text = string.IsNullOrEmpty(nome) ? "(Sem nome)" : nome;
        }
        if (heroiImage)
        {
            heroiImage.sprite = icone;
            heroiImage.preserveAspect = true;
            heroiImage.color = icone ? Color.white : new Color(1, 1, 1, 0);
        }
        PreencherItens(itemSlotsHeroi, itensSprites);
        Debug.Log($"[CombateUI] (Herói) nome='{nome}', icone={(icone ? "ok" : "null")}, itens={itensSprites.Count}");

        // Guarda referência para iniciar o combate
        heroiSelecionadoGO = heroiGO;
        TentarIniciarCombate();
    }

    void RestaurarLayoutCombate()
    {
        // Mostra elementos principais e oculta a área de drop
        AtivarSecao("SlotMonstro", true);
        AtivarSecao("SlotHeroi", true);
        AtivarSecao("Nome", true);
        if (barraVidaHeroi) barraVidaHeroi.gameObject.SetActive(true);
        if (barraVidaMonstro) barraVidaMonstro.gameObject.SetActive(true);
        if (painelVitoria) painelVitoria.SetActive(false);
        if (painelDerrota) painelDerrota.SetActive(false);
        if (dropItemRoot) dropItemRoot.SetActive(false);
    }

    void MostrarApenasDrop(Sprite spriteVisual, ScriptableObject asset, Item itemInventario, int quantidade)
    {
        if (painelCombate) painelCombate.SetActive(true);
        // Oculta elementos
        AtivarSecao("SlotMonstro", false);
        AtivarSecao("SlotHeroi", false);
        AtivarSecao("Nome", false);
        if (barraVidaHeroi) barraVidaHeroi.gameObject.SetActive(false);
        if (barraVidaMonstro) barraVidaMonstro.gameObject.SetActive(false);
        if (painelVitoria) painelVitoria.SetActive(false);
        if (painelDerrota) painelDerrota.SetActive(false);

        if (dropItemRoot)
        {
            dropItemRoot.SetActive(true);
            if (dropItemImage) {
                var sprite = spriteVisual;
                if (!sprite && asset)
                    sprite = ExtrairSpriteDoItem(asset);
                dropItemImage.sprite = sprite;
                dropItemImage.preserveAspect = true;
                dropItemImage.color = sprite ? Color.white : Color.clear;

                // Configura o componente de drag para carregar o payload do item (somente se houver Item de inventário)
                var draggable = dropItemImage.GetComponent<DraggableDropItem>();
                if (asset != null)
                {
                    if (!draggable) draggable = dropItemImage.gameObject.AddComponent<DraggableDropItem>();
                    draggable.item = itemInventario; // pode ser nulo se asset não for Item
                    draggable.asset = asset;
                    draggable.quantidade = Mathf.Max(1, quantidade);
                    draggable.sourceImage = dropItemImage;
                }
                else if (draggable)
                {
                    // Se o drop não for um Item de inventário, remove o arrastar para evitar confusão
                    Destroy(draggable);
                }
            }
        }
    }

    void PreencherItens(List<Image> slots, IList<Sprite> itens)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Count; i++)
        {
            var img = slots[i];
            bool has = itens != null && i < itens.Count && itens[i] != null;

            img.transform.gameObject.SetActive(has);

            if (has)
            {
                img.enabled = true;
                img.sprite = itens[i];
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
            }
        }
    }

    void TentarIniciarCombate()
    {
        if (!combateSistema) return;
        if (!heroiSelecionadoGO && heroiPrefabPadrao) heroiSelecionadoGO = heroiPrefabPadrao;
        if (heroiSelecionadoGO && monstroSelecionadoPrefab)
        {
            combateSistema.Iniciar(heroiSelecionadoGO, monstroSelecionadoPrefab);
            // Após iniciar combate, barras recebem valores iniciais
            AtualizarBarrasDeVida(combateSistema.HeroiVidaAtual, combateSistema.HeroiVidaMax, combateSistema.MonstroVidaAtual, combateSistema.MonstroVidaMax);
        }
    }

    // Busca um campo/propriedade Sprite comum no item (ScriptableObject/MB)
    static Sprite ExtrairSpriteDoItem(Object item)
    {
        if (!item) return null;
        var t = item.GetType();
        var f = t.GetField("icone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(item) as Sprite;

        f = t.GetField("iconeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(item) as Sprite;

        f = t.GetField("sprite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(Sprite)) return f.GetValue(item) as Sprite;

        var p = t.GetProperty("icone");
        if (p != null && p.PropertyType == typeof(Sprite)) return p.GetValue(item, null) as Sprite;

        p = t.GetProperty("sprite");
        if (p != null && p.PropertyType == typeof(Sprite)) return p.GetValue(item, null) as Sprite;

        return null;
    }

    public void Fechar()
    {
        if (painelCombate) painelCombate.SetActive(false);
        if (combateSistema) combateSistema.Encerrar();
        if (painelVitoria) painelVitoria.SetActive(false);
        if (painelDerrota) painelDerrota.SetActive(false);
        if (dropItemRoot) dropItemRoot.SetActive(false);
    }

    void AtualizarBarrasDeVida(float heroiVida, float heroiMax, float monstroVida, float monstroMax)
    {
        if (barraVidaHeroi) barraVidaHeroi.SetValores(heroiVida, heroiMax);
        if (barraVidaMonstro) barraVidaMonstro.SetValores(monstroVida, monstroMax);
    }

    void OnDestroy()
    {
        if (combateSistema)
        {
            combateSistema.OnVidaAtualizada -= AtualizarBarrasDeVida;
            combateSistema.OnCombateFinalizado -= OnCombateFinalizado;
        }
    }

    void OnCombateFinalizado(ResultadoCombate resultado)
    {
        // Mostra resultado por 2s, depois apresenta apenas o item drop
        StopAllCoroutines();
        StartCoroutine(SequenciaResultadoEDrop(resultado));
    }

    System.Collections.IEnumerator SequenciaResultadoEDrop(ResultadoCombate resultado)
    {
        if (resultado == ResultadoCombate.HeroiVenceu)
        {
            if (painelDerrota) painelDerrota.SetActive(false);
            if (painelVitoria) painelVitoria.SetActive(true);
        }
        else
        {
            if (painelVitoria) painelVitoria.SetActive(false);
            if (painelDerrota) painelDerrota.SetActive(true);
        }

        yield return new WaitForSeconds(2f);

        if (resultado == ResultadoCombate.HeroiVenceu)
        {
            // Escolhe o primeiro drop e tenta obter sprite visual e Item para inventário
            dropAssetAtual = null;
            dropItemAtual = null;
            dropQuantidadeAtual = 1;
            Sprite dropSpriteVisual = null;
            if (opcaoAtual != null && opcaoAtual.drops != null)
            {
                foreach (var d in opcaoAtual.drops)
                {
                    if (d == null || d.item == null) continue;
                    if (dropSpriteVisual == null)
                        dropSpriteVisual = ExtrairSpriteDoItem(d.item);
                    if (dropAssetAtual == null)
                        dropAssetAtual = d.item; // ScriptableObject genérico
                    if (dropItemAtual == null)
                        dropItemAtual = d.item as Item;
                    // usamos o primeiro válido encontrado
                    break;
                }
            }
            if (!dropSpriteVisual && dropAssetAtual)
                dropSpriteVisual = ExtrairSpriteDoItem(dropAssetAtual);

            if (!dropSpriteVisual)
                Debug.LogWarning("[CombateUI] Nenhum sprite visual de drop encontrado. Verifique se o asset tem campo 'icone' ou 'sprite'.");

            MostrarApenasDrop(dropSpriteVisual, dropAssetAtual, dropItemAtual, dropQuantidadeAtual);
        }
        else
        {
            // Derrota: não há drop; fecha a UI de combate
            Fechar();
        }
    }

    void AtivarSecao(string path, bool ativo)
    {
        var t = transform.Find(path);
        if (t) t.gameObject.SetActive(ativo);
    }
}

[CreateAssetMenu(menuName = "Item")]
public class Item : ScriptableObject
{
    public string nome;
    public Sprite icone; // este campo deve ser preenchido no Inspector
    // ... outros campos ...
}
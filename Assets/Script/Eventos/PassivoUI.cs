using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; // necessário para IEnumerator

public class PassivoUI : MonoBehaviour
{
    public GameObject painelPassivo;     // raiz da UI de passivo (opcional; se nulo usa este GameObject)
    public TMP_Text tituloText;          // opcional
    public TMP_Text descricaoText;       // opcional
    [Header("Heroi")]
    public Image slotHeroiImage;         // imagem onde o retrato do herói será exibido

    [Header("Resultado Visual")]
    [Tooltip("Painel mostrado em caso de vitória (UIVitoria).")]
    public GameObject painelVitoria;
    [Tooltip("Painel mostrado em caso de derrota (UIDerrota).")]
    public GameObject painelDerrota;
    [Tooltip("Texto opcional para depuração da rolagem.")]
    public TMP_Text resultadoText;

    [Header("Drop de Item")]
    [Tooltip("Raiz para mostrar o item dropado (similar ao Combate).")]
    public GameObject dropItemRoot;
    [Tooltip("Image usada para exibir o sprite do item dropado.")]
    public Image dropItemImage;

    [Header("Derrota")]
    [Tooltip("Root onde ficará o botão de finalizar/continuar quando houver derrota.")]
    public GameObject finalizarRoot;
    public Button finalizarButton;

    [Header("Tempos")]
    [Tooltip("Delay antes de realizar a rolagem automática (segundos).")]
    public float delayRolagemSeg = 2f;
    [Tooltip("Tempo que o painel de vitória/derrota fica visível antes de finalizar o evento.")]
    public float delayMostrarResultadoSeg = 2f;
    [Tooltip("Tempo que o item dropado fica visível antes de finalizar o evento.")]
    public float delayMostrarDropSeg = 2f;

    Evento eventoAtual; // armazenado na abertura
    EventoOpcao opcaoAtual;
    bool resolvendo;
    // Cache do primeiro drop (para exibição e drag)
    ScriptableObject ultimoDropAsset; // asset genérico
    Item ultimoDropItem;              // se for Item concreto
    int ultimoDropQuantidade = 1;     // quantidade escolhida
    bool aguardandoDragParaFinalizar; // sinaliza que espera o arrastar do item
    bool finalizado;                  // evita finalizar múltiplas vezes

    void Awake()
    {
        if (painelPassivo == null) painelPassivo = gameObject;
        if (painelPassivo != null) painelPassivo.SetActive(false);
        // Inicializa root de drop se existir
        if (!dropItemRoot)
        {
            var t = transform.Find("DropItem");
            if (t) dropItemRoot = t.gameObject;
        }
        if (dropItemRoot && !dropItemImage)
            dropItemImage = dropItemRoot.GetComponentInChildren<Image>(true);
        if (dropItemRoot) dropItemRoot.SetActive(false);

        // Inicializa botão finalizar (opcional)
        if (!finalizarRoot)
        {
            var tFin = transform.Find("Finalizar");
            if (tFin) finalizarRoot = tFin.gameObject;
        }
        if (!finalizarButton && finalizarRoot)
            finalizarButton = finalizarRoot.GetComponentInChildren<Button>(true);
        if (finalizarRoot) finalizarRoot.SetActive(false);
    }

    public void AbrirOpcaoPassiva(Evento evento, EventoOpcao opcao)
    {
        if (painelPassivo == null) painelPassivo = gameObject;
        painelPassivo.SetActive(true);
        RestaurarLayoutPassivo();
        // Reset de estado por segurança
        finalizado = false;
        aguardandoDragParaFinalizar = false;
        ultimoDropAsset = null;
        ultimoDropItem = null;
        ultimoDropQuantidade = 1;

        // Título: mostrar o título do evento passivo
        if (tituloText) tituloText.text = evento != null ? evento.nomeEvento : "Evento Passivo";
        if (descricaoText) descricaoText.text = opcao != null ? opcao.descricao : string.Empty;

        // Preencher o slot do herói com o retrato do herói selecionado na EventoUI
        PreencherSlotHeroiComSelecionado();

        // Loga modificadores para referência; você pode renderizar uma lista aqui
        if (opcao != null && opcao.efeitosPassivos != null)
        {
            foreach (var mod in opcao.efeitosPassivos)
            {
                Debug.Log($"[PassivoUI] Mod: {mod.atributo} => {mod.valor}");
            }
        }

        eventoAtual = evento;
        opcaoAtual = opcao;

        if (resultadoText != null) resultadoText.text = "";
        MostrarPainelResultado(null); // garante todos ocultos
        StartCoroutine(RolarAutomaticoAposDelay());
    }

    void RestaurarLayoutPassivo()
    {
        // Reativa todos os filhos diretos do painel, e oculta a área de drop/resultado
        if (painelPassivo)
        {
            for (int i = 0; i < painelPassivo.transform.childCount; i++)
            {
                var child = painelPassivo.transform.GetChild(i).gameObject;
                child.SetActive(true);
            }
        }
        if (painelVitoria) painelVitoria.SetActive(false);
        if (painelDerrota) painelDerrota.SetActive(false);
        if (dropItemRoot) dropItemRoot.SetActive(false);
        if (finalizarRoot) finalizarRoot.SetActive(false);
    }

    void PreencherSlotHeroiComSelecionado()
    {
        var ui = EventoUI.EnsureInstance();
        var heroi = ui != null ? ui.heroiSelecionado : null;

        // Tentar localizar automaticamente o Image do SlotHeroi se não foi ligado no Inspector
        if (slotHeroiImage == null)
        {
            var root = painelPassivo != null ? painelPassivo.transform : transform;
            // Primeiro tenta o caminho mais comum pelo layout do usuário
            var heroiTf = root.Find("SlotHeroi/Heroi");
            if (heroiTf != null) slotHeroiImage = heroiTf.GetComponent<Image>();
            if (slotHeroiImage == null)
            {
                var slotTf = root.Find("SlotHeroi");
                if (slotTf != null) slotHeroiImage = slotTf.GetComponentInChildren<Image>();
            }
        }

        if (heroi == null)
        {
            Debug.Log("[PassivoUI] Nenhum herói selecionado para preencher o SlotHeroi.");
            return;
        }

        if (slotHeroiImage == null)
        {
            Debug.LogWarning("[PassivoUI] slotHeroiImage não atribuído e não encontrado na hierarquia (SlotHeroi/Heroi). Configure no Inspector.");
            return;
        }

        var sprite = heroi.GetSpriteRetrato();
        if (sprite != null)
        {
            slotHeroiImage.sprite = sprite;
            slotHeroiImage.preserveAspect = true;
            slotHeroiImage.color = Color.white;
        }
    }

    IEnumerator RolarAutomaticoAposDelay()
    {
        if (resolvendo) yield break;
        resolvendo = true;
        if (delayRolagemSeg > 0f) yield return new WaitForSeconds(delayRolagemSeg);
        ResolverPassivoInterno();
        resolvendo = false;
    }

    // Lógica: pega primeiro modificador como CD (classe de dificuldade). Rola d20 (1..20) + atributo.
    void ResolverPassivoInterno()
    {
        if (opcaoAtual == null)
        {
            Debug.LogWarning("[PassivoUI] ResolverPassivoInterno sem opcaoAtual.");
            return;
        }
        var ui = EventoUI.EnsureInstance();
        var heroiSel = ui != null ? ui.heroiSelecionado : null;
        if (heroiSel == null)
        {
            Debug.LogWarning("[PassivoUI] Nenhum herói selecionado.");
            if (resultadoText) resultadoText.text = "Selecione um herói.";
            FinalizarDepois(ui, false); // derrota automática
            return;
        }
        var mods = opcaoAtual.efeitosPassivos;
        if (mods == null || mods.Count == 0)
            Debug.LogWarning("[PassivoUI] Opção passiva sem modificadores (CD=0)." );

        var primeiro = (mods != null && mods.Count > 0) ? mods[0] : null;
        var atributo = primeiro != null ? primeiro.atributo : TipoAtributo.Forca;
        int cd = primeiro != null ? primeiro.valor : 0;

        int valorAtributo = 0;
        var compAttr = heroiSel.GetComponent<HeroiAtributos>();
        if (compAttr != null) valorAtributo = compAttr.GetValorAtributo(atributo);
        else Debug.LogWarning("[PassivoUI] HeroiAtributos ausente; atributo=0");

        int rolagem = Random.Range(1, 21);
        int total = rolagem + valorAtributo;
        bool sucesso = total >= cd;
        string msg = $"Rolagem d20={rolagem} + {atributo}({valorAtributo}) = {total} vs CD {cd} => {(sucesso ? "SUCESSO" : "FALHA")}";
        Debug.Log("[PassivoUI] " + msg);
        if (resultadoText) resultadoText.text = msg;

        // Mostra painel resultado
        MostrarPainelResultado(sucesso);

        if (sucesso)
        {
            // Prepara apenas o drop visual (não adiciona ao inventário do herói)
            PrepararDropVisual(opcaoAtual);
            StartCoroutine(SequenciaResultadoEDropPassivo(ui));
        }
        else
        {
            // Derrota: após mostrar painel por alguns segundos, exibir apenas botão de finalizar
            StartCoroutine(SequenciaDerrotaMostrarFinalizar());
        }
    }

    void MostrarPainelResultado(bool? sucesso)
    {
        if (painelVitoria) painelVitoria.SetActive(sucesso == true);
        if (painelDerrota) painelDerrota.SetActive(sucesso == false);
    }

    void PrepararDropVisual(EventoOpcao opc)
    {
        // Não adiciona nada ao inventário aqui; apenas escolhe e configura o drop visual
        ultimoDropAsset = null;
        ultimoDropItem = null;
        ultimoDropQuantidade = 1;
        if (opc == null || opc.drops == null) return;

        // Escolhe o primeiro drop que passar na chance; se nenhum passar, usa o primeiro válido
        ScriptableObject candidato = null;
        int quantidade = 1;
        foreach (var d in opc.drops)
        {
            if (d == null || d.item == null) continue;
            candidato ??= d.item;
            if (Random.value <= d.chance)
            {
                candidato = d.item;
                quantidade = Random.Range(d.quantidadeMin, d.quantidadeMax + 1);
                break;
            }
        }
        if (candidato != null)
        {
            ultimoDropAsset = candidato;
            ultimoDropItem = candidato as Item;
            ultimoDropQuantidade = Mathf.Max(1, quantidade);
        }
    }

    void FinalizarDepois(EventoUI ui, bool sucesso)
    {
        StartCoroutine(FinalizarAposDelay(ui));
    }

    IEnumerator FinalizarAposDelay(EventoUI ui)
    {
        if (delayMostrarResultadoSeg > 0f) yield return new WaitForSeconds(delayMostrarResultadoSeg);
        if (ui != null) ui.FinalizarEventoAtual();
    }

    // Sequência para vitória passiva: mostra painel vitória, espera, mostra item drop, espera, finaliza
    IEnumerator SequenciaResultadoEDropPassivo(EventoUI ui)
    {
        // Já estamos com painelVitoria ativo
        if (delayMostrarResultadoSeg > 0f) yield return new WaitForSeconds(delayMostrarResultadoSeg);

        // Obter sprite de algum drop para exibir (similar ao combate)
        Sprite dropSprite = ObterSpriteDoCacheOuBusca();
        MostrarApenasDrop(dropSprite);
        // Se temos sprite e componente arrastável, aguardamos o drag para finalizar.
        if (dropSprite != null && dropItemImage != null && dropItemImage.GetComponent<DraggableDropItem>() != null)
        {
            aguardandoDragParaFinalizar = true;
            yield break; // não fecha por tempo; exige arrastar
        }
        // Sem drop visual válido: finaliza imediatamente
        FinalizarEventoInterno();
    }

    void MostrarApenasDrop(Sprite sprite)
    {
        if (!painelPassivo) return;
        // Oculta todos os filhos diretos exceto o root de drop
        for (int i = 0; i < painelPassivo.transform.childCount; i++)
        {
            var child = painelPassivo.transform.GetChild(i).gameObject;
            if (dropItemRoot != null && child == dropItemRoot) continue;
            // Mantém o próprio GameObject se painelPassivo == this.gameObject
            if (child == dropItemRoot) continue;
            if (child == painelVitoria || child == painelDerrota) { child.SetActive(false); continue; }
            if (finalizarRoot != null && child == finalizarRoot) { child.SetActive(false); continue; }
            if (child == dropItemRoot) continue;
            child.SetActive(false);
        }
        MostrarPainelResultado(null); // garante vitória/derrota ocultos
        if (finalizarRoot) finalizarRoot.SetActive(false);
        if (!dropItemRoot) return;
        dropItemRoot.SetActive(true);
        if (dropItemImage)
        {
            dropItemImage.sprite = sprite;
            dropItemImage.preserveAspect = true;
            dropItemImage.color = sprite ? Color.white : Color.clear;
            TornarDropArrastavel();
        }
    }

    IEnumerator SequenciaDerrotaMostrarFinalizar()
    {
        if (delayMostrarResultadoSeg > 0f) yield return new WaitForSeconds(delayMostrarResultadoSeg);
        MostrarApenasBotaoFinalizar();
    }

    void MostrarApenasBotaoFinalizar()
    {
        if (!painelPassivo) return;
        for (int i = 0; i < painelPassivo.transform.childCount; i++)
        {
            var child = painelPassivo.transform.GetChild(i).gameObject;
            if (finalizarRoot != null && child == finalizarRoot) continue;
            if (child == painelVitoria || child == painelDerrota) { child.SetActive(false); continue; }
            if (dropItemRoot != null && child == dropItemRoot) { child.SetActive(false); continue; }
            child.SetActive(false);
        }
        MostrarPainelResultado(null);
        if (dropItemRoot) dropItemRoot.SetActive(false);
        if (!finalizarRoot) return;
        finalizarRoot.SetActive(true);
        if (finalizarButton)
        {
            finalizarButton.onClick.RemoveAllListeners();
            finalizarButton.onClick.AddListener(() => { FinalizarEventoInterno(); });
        }
    }

    void TornarDropArrastavel()
    {
        if (!dropItemImage) return;
        var draggable = dropItemImage.GetComponent<DraggableDropItem>();
        if (ultimoDropAsset != null)
        {
            if (!draggable) draggable = dropItemImage.gameObject.AddComponent<DraggableDropItem>();
            draggable.asset = ultimoDropAsset;
            draggable.item = ultimoDropItem; // pode ser nulo se asset não for Item
            draggable.quantidade = Mathf.Max(1, ultimoDropQuantidade);
            draggable.sourceImage = dropItemImage;
            // Adiciona finalizador de drag
            var fin = dropItemImage.GetComponent<PassivoDropFinalizador>();
            if (!fin) fin = dropItemImage.gameObject.AddComponent<PassivoDropFinalizador>();
            fin.passivo = this;
        }
        else if (draggable)
        {
            Destroy(draggable); // sem asset válido, remove
        }
    }

    Sprite ObterSpriteDoCacheOuBusca()
    {
        if (ultimoDropAsset != null)
        {
            var spr = ExtrairSpriteGenerico(ultimoDropAsset);
            if (spr) return spr;
        }
        return ObterPrimeiroDropSprite(opcaoAtual);
    }

    Sprite ObterPrimeiroDropSprite(EventoOpcao opc)
    {
        if (opc == null || opc.drops == null) return null;
        foreach (var d in opc.drops)
        {
            if (d == null || d.item == null) continue;
            var spr = ExtrairSpriteGenerico(d.item);
            if (spr) return spr;
        }
        return null;
    }

    // Similar à lógica de ExtrairSpriteDoItem em combate
    static Sprite ExtrairSpriteGenerico(Object item)
    {
        if (!item) return null;
        var t = item.GetType();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        string[] campos = { "icone", "iconeItem", "sprite" };
        foreach (var nome in campos)
        {
            var f = t.GetField(nome, flags);
            if (f != null && f.FieldType == typeof(Sprite))
            {
                var val = f.GetValue(item) as Sprite;
                if (val) return val;
            }
            var p = t.GetProperty(nome, flags);
            if (p != null && p.PropertyType == typeof(Sprite))
            {
                var val = p.GetValue(item, null) as Sprite;
                if (val) return val;
            }
        }
        return null;
    }

    // Chamado pelo componente de drag quando usuário termina de arrastar
    public void FinalizarPorDrag()
    {
        if (!aguardandoDragParaFinalizar || finalizado) return;
        Debug.Log("[PassivoUI] Item arrastado, finalizando evento passivo.");
        FinalizarEventoInterno();
    }

    void FinalizarEventoInterno()
    {
        if (finalizado) return;
        finalizado = true;
        aguardandoDragParaFinalizar = false;
        Fechar();
        var ui = EventoUI.EnsureInstance();
        if (ui != null) ui.FinalizarEventoAtual();
    }

    public void Fechar()
    {
        if (painelPassivo) painelPassivo.SetActive(false);
        if (dropItemRoot) dropItemRoot.SetActive(false);
        if (painelVitoria) painelVitoria.SetActive(false);
        if (painelDerrota) painelDerrota.SetActive(false);
    }
}

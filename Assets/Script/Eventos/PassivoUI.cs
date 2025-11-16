using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PassivoUI : MonoBehaviour
{
    public GameObject painelPassivo;     // raiz da UI de passivo (opcional; se nulo usa este GameObject)
    public TMP_Text tituloText;          // opcional
    public TMP_Text descricaoText;       // opcional
    [Header("Heroi")]
    public Image slotHeroiImage;         // imagem onde o retrato do herói será exibido

    void Awake()
    {
        if (painelPassivo == null) painelPassivo = gameObject;
        if (painelPassivo != null) painelPassivo.SetActive(false);
    }

    public void AbrirOpcaoPassiva(Evento evento, EventoOpcao opcao)
    {
        if (painelPassivo == null) painelPassivo = gameObject;
        painelPassivo.SetActive(true);

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
}

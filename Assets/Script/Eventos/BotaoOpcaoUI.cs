using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BotaoOpcaoUI : MonoBehaviour
{
    [Header("UI Interna do Botão")]
    public TextMeshProUGUI textoOpcao;
    public Image iconeOpcao; // Opcional, caso queira mostrar ícone

    private EventoOpcao dadosOpcao;
    private EventoUI manager;

    public void Configurar(EventoOpcao opcao, EventoUI uiManager)
    {
        this.dadosOpcao = opcao;
        this.manager = uiManager;

        // Configura o texto do botão (Ex: "Lutar", "Convencer")
        if (textoOpcao != null) 
            textoOpcao.text = opcao.nomeOpcao;

        // Se tiver ícone configurado no ScriptableObject da opção, usa ele
        if (iconeOpcao != null && opcao.icone != null && opcao.usarIconeDaOpcao)
        {
            iconeOpcao.sprite = opcao.icone;
            iconeOpcao.gameObject.SetActive(true);
        }
    }

    // Vincule essa função ao OnClick do botão no Inspector do Prefab
    public void AoClicar()
    {
        if (manager != null)
        {
            manager.ResolverOpcao(dadosOpcao);
        }
    }
}
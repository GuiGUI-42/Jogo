using UnityEngine;
using TMPro;

public class PassivoUI : MonoBehaviour
{
    public GameObject painelPassivo;     // raiz da UI de passivo (opcional; se nulo usa este GameObject)
    public TMP_Text tituloText;          // opcional
    public TMP_Text descricaoText;       // opcional

    void Awake()
    {
        if (painelPassivo == null) painelPassivo = gameObject;
        if (painelPassivo != null) painelPassivo.SetActive(false);
    }

    public void AbrirOpcaoPassiva(Evento evento, EventoOpcao opcao)
    {
        if (painelPassivo == null) painelPassivo = gameObject;
        painelPassivo.SetActive(true);

        if (tituloText) tituloText.text = opcao != null && !string.IsNullOrEmpty(opcao.nomeOpcao) ? opcao.nomeOpcao : (evento != null ? evento.nomeEvento : "Opção Passiva");
        if (descricaoText) descricaoText.text = opcao != null ? opcao.descricao : string.Empty;

        // Loga modificadores para referência; você pode renderizar uma lista aqui
        if (opcao != null && opcao.efeitosPassivos != null)
        {
            foreach (var mod in opcao.efeitosPassivos)
            {
                Debug.Log($"[PassivoUI] Mod: {mod.atributo} => {mod.valor}");
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Configurações Iniciais")]
    public float reputacaoAtual = 500f; 
    public float reputacaoMaxima = 1000f; 
    public int ouroAtual = 200; 

    [Header("UI")]
    public TMP_Text textoOuro;        
    public TMP_Text textoReputacao;
    public Image barraReputacao;      

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start() { AtualizarUI(); }

    // --- ECONOMIA ---
    public bool TemOuroSuficiente(int custo)
    {
        return ouroAtual >= custo;
    }

    public void GastarOuro(int custo)
    {
        if (ouroAtual >= custo)
        {
            ouroAtual -= custo;
            AtualizarUI();
        }
    }

    // --- NOVO: VENDER ---
    public void GanharOuro(int quantidade)
    {
        ouroAtual += quantidade;
        AtualizarUI();
    }
    // --------------------

    public void FinalizarEvento(bool vitoria, int ouroRecompensa, float repRecompensa)
    {
        if (vitoria)
        {
            ouroAtual += ouroRecompensa;
            reputacaoAtual += repRecompensa;
        }
        else
        {
            float punicao = repRecompensa / 2f;
            reputacaoAtual -= punicao;
        }
        reputacaoAtual = Mathf.Clamp(reputacaoAtual, 0, reputacaoMaxima);
        AtualizarUI();
    }

    void AtualizarUI()
    {
        if(textoOuro != null) textoOuro.text = "Ouro: " + ouroAtual.ToString();
        if(textoReputacao != null) textoReputacao.text = $"{reputacaoAtual:F0} / {reputacaoMaxima}";
        if (barraReputacao != null) barraReputacao.fillAmount = reputacaoAtual / reputacaoMaxima;
    }
}
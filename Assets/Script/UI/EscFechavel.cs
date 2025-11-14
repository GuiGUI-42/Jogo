using UnityEngine;

/// <summary>
/// Anexe este componente a um GameObject representando um painel (ou window) que deve fechar com ESC.
/// Configure "raiz" para o objeto que será desativado (pode ser o próprio). Opcional prioridade.
/// </summary>
public class EscFechavel : MonoBehaviour, IEscFechavel
{
    [Tooltip("Objeto que será desativado ao fechar. Se vazio usa este GameObject.")]
    public GameObject raiz;

    [Tooltip("Prioridade: painéis com valor mais alto fecham primeiro no modo 'CloseOneHighestPriority'.")]
    public int prioridade = 0;

    [Tooltip("Fechar também se clicar fora (futuro - não implementado).")]
    public bool placeholderFeature; // reservado para expansão

    public bool EstaAberto => (raiz ? raiz.activeInHierarchy : gameObject.activeInHierarchy);
    public int Prioridade => prioridade;

    void Awake()
    {
        if (!raiz) raiz = gameObject;
    }

    void OnEnable()
    {
        EscUIManager.Registrar(this);
    }

    void OnDisable()
    {
        EscUIManager.Desregistrar(this);
    }

    public void Fechar()
    {
        if (raiz) raiz.SetActive(false);
    }
}

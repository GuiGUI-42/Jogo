using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida genérica. Pode usar:
/// 1) Image com tipo Filled (fillAmount).
/// 2) Uma RectTransform (ajusta largura via sizeDelta.x).
/// 3) Escala local em X (opcional).
/// Ordem de preferência: Filled -> RectTransform -> scale.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Tooltip("Valor atual da vida.")] public float valorAtual;
    [Tooltip("Valor máximo da vida.")] public float valorMax = 100f;

    [Header("Opções")]
    [Tooltip("Atualizar automaticamente fillAmount se houver Image Filled.")] public bool usarFillAmount = true;
    [Tooltip("Atualizar sizeDelta.x se houver RectTransform e não usar Fill.")] public bool usarSizeDelta = true;
    [Tooltip("Atualizar escala local X se não houver outros modos.")] public bool usarEscala = true;
    [Tooltip("Largura base para sizeDelta (se zero pega tamanho atual na primeira atualização).")]
    public float larguraBase = 0f;

    Image img;
    RectTransform rt;

    void Awake()
    {
        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        if (larguraBase <= 0f && rt) larguraBase = rt.sizeDelta.x;
    }

    public void SetValores(float atual, float max)
    {
        valorMax = max <= 0 ? 1 : max;
        valorAtual = Mathf.Clamp(atual, 0, valorMax);
        AtualizarVisual();
    }

    void AtualizarVisual()
    {
        float ratio = valorAtual / (valorMax <= 0 ? 1 : valorMax);
        ratio = Mathf.Clamp01(ratio);

        if (usarFillAmount && img && img.type == Image.Type.Filled)
        {
            img.fillAmount = ratio;
            return;
        }

        if (usarSizeDelta && rt)
        {
            rt.sizeDelta = new Vector2(larguraBase * ratio, rt.sizeDelta.y);
            return;
        }

        if (usarEscala)
        {
            transform.localScale = new Vector3(ratio, transform.localScale.y, transform.localScale.z);
        }
    }
}

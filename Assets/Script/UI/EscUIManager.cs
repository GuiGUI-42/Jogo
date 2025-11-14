using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Manager central que escuta ESC e fecha UIs registradas.
/// Adicione em um GameObject persistente (ex: Canvas raiz).
/// </summary>
public class EscUIManager : MonoBehaviour
{
    public enum EscCloseMode { CloseAll, CloseOneHighestPriority }

    [Tooltip("Como fechar ao apertar ESC: todos os abertos ou só o de maior prioridade.")] 
    public EscCloseMode closeMode = EscCloseMode.CloseOneHighestPriority;

    [Tooltip("Fechar múltiplos em cadeia até não restar nenhum aberto (apenas CloseOneHighestPriority)")] 
    public bool chainClose = false;

    private static readonly HashSet<IEscFechavel> registrados = new HashSet<IEscFechavel>();

    public static void Registrar(IEscFechavel ui)
    {
        if (ui != null) registrados.Add(ui);
    }
    public static void Desregistrar(IEscFechavel ui)
    {
        if (ui != null) registrados.Remove(ui);
    }

    void Update()
    {
        if (EscapePressed())
        {
            FecharPorEsc();
        }
    }

    bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    void FecharPorEsc()
    {
        if (registrados.Count == 0) return;
        var abertos = registrados.Where(r => r != null && r.EstaAberto).ToList();
        if (abertos.Count == 0) return;

        if (closeMode == EscCloseMode.CloseAll)
        {
            foreach (var ui in abertos) ui.Fechar();
            return;
        }

        // Fecha o(s) de maior prioridade
        while (true)
        {
            var alvo = abertos.OrderByDescending(r => r.Prioridade).FirstOrDefault();
            if (alvo == null) break;
            alvo.Fechar();
            if (!chainClose) break;
            abertos = registrados.Where(r => r != null && r.EstaAberto).ToList();
            if (abertos.Count == 0) break;
        }
    }
}

/// <summary>
/// Interface para UIs fecháveis via ESC.
/// </summary>
public interface IEscFechavel
{
    bool EstaAberto { get; }
    int Prioridade { get; }
    void Fechar();
}

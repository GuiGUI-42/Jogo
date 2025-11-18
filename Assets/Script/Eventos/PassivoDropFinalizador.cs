using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Valida fechamento do PassivoUI somente se o item foi solto fora do painel do evento
// (em um alvo externo, como outro inventário/bolsa), evitando fechar em missclicks.
public class PassivoDropFinalizador : MonoBehaviour, IEndDragHandler
{
    [HideInInspector] public PassivoUI passivo;

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!passivo) return;
        StartCoroutine(ValidarPosDrop(eventData));
    }

    IEnumerator ValidarPosDrop(PointerEventData eventData)
    {
        // Aguarda fim do frame para permitir que OnDrop reprocesse parent/posicionamento
        yield return null;

        if (!passivo) yield break;

        var painel = passivo.painelPassivo ? passivo.painelPassivo.transform : passivo.transform;

        // 1) Se este item foi reparentado para fora do painel, consideramos coleta válida
        if (!IsUnder(transform, painel))
        {
            passivo.FinalizarPorDrag();
            yield break;
        }

        // 2) Se o ponteiro terminou sobre um alvo com IDropHandler fora do painel, consideramos válido
        if (EventSystem.current != null)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                var go = r.gameObject;
                if (!go) continue;
                if (IsUnder(go.transform, painel)) continue; // ainda dentro do painel do evento
                if (go.GetComponent(typeof(IDropHandler)) != null)
                {
                    passivo.FinalizarPorDrag();
                    yield break;
                }
            }
        }
        // Caso contrário, não fecha (missclick/slot inválido)
    }

    static bool IsUnder(Transform child, Transform potentialParent)
    {
        var t = child;
        while (t != null)
        {
            if (t == potentialParent) return true;
            t = t.parent;
        }
        return false;
    }
}

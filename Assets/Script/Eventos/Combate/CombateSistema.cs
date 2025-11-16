using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombateSistema : MonoBehaviour
{
    class CombatenteRuntime
    {
        public string nome;
        public GameObject origem;
        public HeroiAtributos atributos;
        public Heroi baseHeroi;
        public float vidaMax;
        public float vidaAtual;
        public ItemCombate[] itens;
        public readonly Dictionary<ItemCombate, float> ultimoUso = new();
    }

    CombatenteRuntime heroi;
    CombatenteRuntime monstro;
    Coroutine combateCo;
    bool emCombate;

    // Evento de atualização de vida: heroiVida, heroiMax, monstroVida, monstroMax
    public System.Action<float, float, float, float> OnVidaAtualizada;

    public float HeroiVidaAtual => heroi?.vidaAtual ?? 0f;
    public float HeroiVidaMax => heroi?.vidaMax ?? 0f;
    public float MonstroVidaAtual => monstro?.vidaAtual ?? 0f;
    public float MonstroVidaMax => monstro?.vidaMax ?? 0f;
    public bool EmCombate => emCombate;

    // Sinaliza fim do combate
    public System.Action<ResultadoCombate> OnCombateFinalizado;

    public void Iniciar(GameObject heroiGO, GameObject monstroPrefab)
    {
        Encerrar();
        heroi = MontarCombatente(heroiGO);
        monstro = MontarCombatente(monstroPrefab);

        if (heroi == null || monstro == null)
        {
            Debug.LogError("[Combate] Falha ao montar combatentes.");
            return;
        }

        combateCo = StartCoroutine(RotinaCombate());
        DispararVidaAtualizada();
    }

    public void Encerrar()
    {
        if (combateCo != null)
        {
            StopCoroutine(combateCo);
            combateCo = null;
        }
        emCombate = false;
        heroi = null;
        monstro = null;
        DispararVidaAtualizada();
    }

    CombatenteRuntime MontarCombatente(GameObject go)
    {
        if (!go) return null;
        var at = go.GetComponent<HeroiAtributos>();
        if (!at)
        {
            Debug.LogError("[Combate] GameObject não possui HeroiAtributos: " + go.name);
            return null;
        }

        var baseH = at.baseAtributos;
        if (!baseH)
        {
            Debug.LogError("[Combate] baseAtributos nulo em: " + go.name);
            return null;
        }

        var c = new CombatenteRuntime
        {
            nome = string.IsNullOrEmpty(baseH.nomeHeroi) ? go.name : baseH.nomeHeroi,
            origem = go,
            atributos = at,
            baseHeroi = baseH,
            vidaMax = Mathf.Max(1, baseH.vitalidade) * 10f,
        };
        c.vidaAtual = c.vidaMax;
        // Coleta itens de combate tanto dos itens iniciais quanto dos slotsInventario atuais
        c.itens = ColetarItensCombate(at);
        c.ultimoUso.Clear();
        foreach (var item in c.itens)
        {
            if (!item) continue;
            // Registra último uso para respeitar cooldown inicial (espera o primeiro intervalo)
            c.ultimoUso[item] = Time.time;
        }
        return c;
    }

    ItemCombate[] ColetarItensCombate(HeroiAtributos at)
    {
        if (!at) return new ItemCombate[0];
        var lista = new List<ItemCombate>();
        // Apenas slots atuais do herói
        if (at.slotsInventario != null)
        {
            foreach (var so in at.slotsInventario)
            {
                if (!so) continue;
                if (so is ItemCombate ic && !lista.Contains(ic)) lista.Add(ic);
            }
        }
        return lista.ToArray();
    }

    IEnumerator RotinaCombate()
    {
        emCombate = true;
        Debug.Log("[Combate] Iniciado: " + heroi.nome + " vs " + monstro.nome);
        var wait = new WaitForSeconds(0.1f);
        while (emCombate)
        {
            if (TickCombatente(heroi, monstro)) yield return wait;
            if (!emCombate) break;
            if (TickCombatente(monstro, heroi)) yield return wait;
            if (!emCombate) break;
            DispararVidaAtualizada();
            yield return wait;
        }
    }

    bool TickCombatente(CombatenteRuntime atacante, CombatenteRuntime defensor)
    {
        if (!emCombate) return false;
        if (atacante.itens == null || atacante.itens.Length == 0) return false;

        bool houveAcao = false;
        foreach (var item in atacante.itens)
        {
            if (!item) continue;
            float ultimoUso = atacante.ultimoUso.TryGetValue(item, out var u) ? u : 0f;
            if (!item.PodeAtivar(ultimoUso)) continue;

            int dano = 0;
            // Danos físico e elemental
            dano += item.CalcularDanoFisico(atacante.baseHeroi);
            dano += item.CalcularDanoElemental(atacante.baseHeroi);

            // Cura do usuário baseada na Vitalidade
            int cura = item.CalcularCura(atacante.baseHeroi);

            if (dano <= 0 && cura <= 0) continue;

            if (dano > 0)
            {
                defensor.vidaAtual = Mathf.Max(0, defensor.vidaAtual - dano);
            }

            if (cura > 0)
            {
                atacante.vidaAtual = Mathf.Min(atacante.vidaMax, atacante.vidaAtual + cura);
            }

            atacante.ultimoUso[item] = Time.time; // registra uso
            houveAcao = true;

            if (dano > 0 && cura > 0)
            {
                Debug.Log($"[Combate] {atacante.nome} usa {item.nomeItem}: cura +{cura} | dano -{dano} em {defensor.nome}. Vidas => {atacante.nome}: {atacante.vidaAtual}/{atacante.vidaMax}, {defensor.nome}: {defensor.vidaAtual}/{defensor.vidaMax}");
            }
            else if (dano > 0)
            {
                Debug.Log($"[Combate] {atacante.nome} usa {item.nomeItem} -> {defensor.nome} (-{dano}) Vida: {defensor.vidaAtual}/{defensor.vidaMax}");
            }
            else if (cura > 0)
            {
                Debug.Log($"[Combate] {atacante.nome} usa {item.nomeItem} e cura +{cura} (Vida: {atacante.vidaAtual}/{atacante.vidaMax})");
            }

            if (defensor.vidaAtual <= 0)
            {
                Debug.Log($"[Combate] {defensor.nome} foi derrotado! Vencedor: {atacante.nome}");
                emCombate = false;
                DispararVidaAtualizada();
                var resultado = (atacante == heroi) ? ResultadoCombate.HeroiVenceu : ResultadoCombate.MonstroVenceu;
                OnCombateFinalizado?.Invoke(resultado);
                break;
            }
            DispararVidaAtualizada();
        }
        return houveAcao;
    }

    void DispararVidaAtualizada()
    {
        OnVidaAtualizada?.Invoke(HeroiVidaAtual, HeroiVidaMax, MonstroVidaAtual, MonstroVidaMax);
    }
}

public enum ResultadoCombate
{
    HeroiVenceu,
    MonstroVenceu
}

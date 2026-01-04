using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization; // Necessário

public class CombateSistema : MonoBehaviour
{
    struct BuffRuntime
    {
        public int mascaraAlvo; 
        public TipoElemento elemento;
        public float valor;
    }

    class QueimaduraInstance
    {
        public float danoAtual;
        public float proximoTick;
    }
    
    class VenenoInstance
    {
        public float danoPorTick;
        public float proximoTick;
        public float tempoFim;
    }

    class CombatenteRuntime
    {
        public string nome; // Nome traduzido em tempo real
        public GameObject origem;
        public HeroiAtributos atributos;
        public Heroi baseHeroi; 
        
        public float vidaMax;
        public float vidaAtual;
        public float armaduraAtual;

        public ItemCombate[] slotsItens; 
        public Dictionary<int, float> ultimoUso = new Dictionary<int, float>();

        public List<BuffRuntime> buffsPermanentes = new List<BuffRuntime>();

        public float tempoAtordoado = 0f;
        public float tempoMolhado = 0f;
        public float tempoVulneravel = 0f;
        public float tempoInflamavel = 0f;
        public float tempoCorrosao = 0f;
        public float tempoInvulneravel = 0f;

        public List<QueimaduraInstance> queimaduras = new List<QueimaduraInstance>();
        public List<VenenoInstance> venenos = new List<VenenoInstance>();

        public bool IsAtordoado => tempoAtordoado > 0f;
    }

    CombatenteRuntime heroi;
    CombatenteRuntime monstro;
    Coroutine combateCo;
    bool emCombate;

    public System.Action<float, float, float, float, float, float> OnVidaAtualizada;
    public System.Action<ResultadoCombate> OnCombateFinalizado;

    // Getters
    public float HeroiVidaAtual => heroi?.vidaAtual ?? 0f;
    public float HeroiArmaduraAtual => heroi?.armaduraAtual ?? 0f;
    public float HeroiVidaMax => heroi?.vidaMax ?? 0f;
    public float MonstroVidaAtual => monstro?.vidaAtual ?? 0f;
    public float MonstroArmaduraAtual => monstro?.armaduraAtual ?? 0f;
    public float MonstroVidaMax => monstro?.vidaMax ?? 0f;

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
        if (combateCo != null) StopCoroutine(combateCo);
        combateCo = null;
        emCombate = false;
        heroi = null;
        monstro = null;
    }

    CombatenteRuntime MontarCombatente(GameObject go)
    {
        if (!go) return null;
        var at = go.GetComponent<HeroiAtributos>();
        if (!at) return null;
        var baseH = at.baseAtributos;
        if (!baseH) return null;

        var c = new CombatenteRuntime
        {
            // --- TRADUÇÃO: Pega o texto da chave ---
            nome = (baseH.nomeHeroi.IsEmpty) ? go.name : baseH.nomeHeroi.GetLocalizedString(),
            
            origem = go,
            atributos = at,
            baseHeroi = baseH,
            vidaMax = Mathf.Max(1, baseH.vitalidade) * 10f,
            armaduraAtual = 0f,
            slotsItens = new ItemCombate[at.slotsInventario.Length]
        };
        c.vidaAtual = c.vidaMax;

        for(int i=0; i < at.slotsInventario.Length; i++)
        {
            if (at.slotsInventario[i] is ItemCombate itemC)
            {
                c.slotsItens[i] = itemC;
                c.ultimoUso[i] = -9999f; 
            }
            else
            {
                c.slotsItens[i] = null;
            }
        }
        return c;
    }

    IEnumerator RotinaCombate()
    {
        emCombate = true;
        Debug.Log($"[Combate] Iniciado: {heroi.nome} vs {monstro.nome}");
        
        while (emCombate)
        {
            float dt = Time.deltaTime;
            
            TickStatus(heroi, dt);
            TickStatus(monstro, dt);
            
            if (VerificarMorte()) break;

            if (!heroi.IsAtordoado) ProcessarItens(heroi, monstro);
            if (VerificarMorte()) break;

            if (!monstro.IsAtordoado) ProcessarItens(monstro, heroi);
            if (VerificarMorte()) break;

            DispararVidaAtualizada();
            yield return null; 
        }
    }

    void TickStatus(CombatenteRuntime c, float dt)
    {
        if (c.tempoAtordoado > 0) c.tempoAtordoado -= dt;
        if (c.tempoMolhado > 0) c.tempoMolhado -= dt;
        if (c.tempoVulneravel > 0) c.tempoVulneravel -= dt;
        if (c.tempoInflamavel > 0) c.tempoInflamavel -= dt;
        if (c.tempoCorrosao > 0) c.tempoCorrosao -= dt;
        if (c.tempoInvulneravel > 0) c.tempoInvulneravel -= dt;

        for (int i = c.queimaduras.Count - 1; i >= 0; i--)
        {
            var q = c.queimaduras[i];
            if (Time.time >= q.proximoTick)
            {
                AplicarDanoBruto(c, q.danoAtual, true); 
                q.danoAtual -= 1f;
                q.proximoTick = Time.time + 0.5f;
                if (q.danoAtual <= 0) c.queimaduras.RemoveAt(i);
            }
        }

        for (int i = c.venenos.Count - 1; i >= 0; i--)
        {
            var v = c.venenos[i];
            if (Time.time >= v.proximoTick)
            {
                AplicarDanoBruto(c, v.danoPorTick, false);
                v.proximoTick = Time.time + 1.0f;
            }
            if (Time.time >= v.tempoFim) c.venenos.RemoveAt(i);
        }
    }

    void ProcessarItens(CombatenteRuntime atacante, CombatenteRuntime defensor)
    {
        for (int i = 0; i < atacante.slotsItens.Length; i++)
        {
            ItemCombate item = atacante.slotsItens[i];
            if (item == null) continue;

            float cooldownReal = CalcularCooldownReal(atacante, item);
            float ultimo = atacante.ultimoUso.ContainsKey(i) ? atacante.ultimoUso[i] : 0f;

            if ((Time.time - ultimo) >= cooldownReal)
            {
                AtivarItem(atacante, defensor, i, item);
            }
        }
    }

    float CalcularCooldownReal(CombatenteRuntime atacante, ItemCombate item)
    {
        float cd = item.cooldownSegundos;
        foreach (var outroItem in atacante.slotsItens)
        {
            if (outroItem == null || outroItem == item) continue; 
            foreach (var ef in outroItem.efeitos)
            {
                if (ef.modo == ModoEfeito.Passivo && ef.tipoEfeito == TipoEfeitoItem.ReduzirCooldownTipo)
                {
                    if (item.PossuiTipo(ef.parametroExtra)) cd -= ef.valor; 
                }
            }
        }
        return Mathf.Max(0.1f, cd);
    }

    float CalcularBuffDano(CombatenteRuntime atacante, int slotIndexItemAtual, TipoElemento tipoDano)
    {
        float buffTotal = 0;
        ItemCombate itemAtual = atacante.slotsItens[slotIndexItemAtual];

        foreach (var buff in atacante.buffsPermanentes)
        {
            if (buff.elemento == tipoDano && itemAtual.PossuiTipo(buff.mascaraAlvo))
            {
                buffTotal += buff.valor;
            }
        }

        for (int i = 0; i < atacante.slotsItens.Length; i++)
        {
            if (i == slotIndexItemAtual) continue;

            ItemCombate fonteBuff = atacante.slotsItens[i];
            if (fonteBuff == null) continue;

            float ultimoUsoFonte = atacante.ultimoUso.ContainsKey(i) ? atacante.ultimoUso[i] : -9999f;

            foreach (var ef in fonteBuff.efeitos)
            {
                if (ef.tipoEfeito == TipoEfeitoItem.AumentarDano && ef.elementoAlvo == tipoDano)
                {
                    bool ativo = false;

                    if (ef.modo == ModoEfeito.Passivo) ativo = true;
                    else if (ef.modo == ModoEfeito.Ativo && ef.duracao > 0) 
                    {
                        if (Time.time <= ultimoUsoFonte + ef.duracao) ativo = true;
                    }

                    if (ativo)
                    {
                        if (ef.alvo == AlvoEfeito.Adjacentes)
                        {
                            if (Mathf.Abs(i - slotIndexItemAtual) == 1) buffTotal += ef.valor;
                        }
                        else if (ef.alvo == AlvoEfeito.Usuario)
                        {
                            if (itemAtual.PossuiTipo(ef.parametroExtra)) buffTotal += ef.valor;
                        }
                    }
                }
            }
        }
        return buffTotal;
    }

    void AtivarItem(CombatenteRuntime atacante, CombatenteRuntime defensor, int slotIndex, ItemCombate item)
    {
        atacante.ultimoUso[slotIndex] = Time.time;

        int cura = item.CalcularCura(atacante.baseHeroi);
        int armadura = item.CalcularArmadura(atacante.baseHeroi);

        if (cura > 0) atacante.vidaAtual = Mathf.Min(atacante.vidaMax, atacante.vidaAtual + cura);
        if (armadura > 0) atacante.armaduraAtual += armadura;

        float danoVidaTotalCausado = 0f;

        foreach (var comp in item.danos)
        {
            float valorTotal = item.CalcularDanoComponente(comp, atacante.baseHeroi);
            float buffExtra = CalcularBuffDano(atacante, slotIndex, comp.tipo);
            if (buffExtra > 0) valorTotal += buffExtra;

            if (defensor.tempoVulneravel > 0) valorTotal *= 2f;

            switch (comp.tipo)
            {
                case TipoElemento.Fisico:
                case TipoElemento.Gelo:
                    danoVidaTotalCausado += AplicarDanoBruto(defensor, valorTotal, false);
                    break;

                case TipoElemento.Eletrico:
                    if (defensor.tempoMolhado > 0) valorTotal *= 2f; 
                    danoVidaTotalCausado += AplicarDanoBruto(defensor, valorTotal, false);
                    break;

                case TipoElemento.Fogo:
                    if (defensor.tempoInflamavel > 0) valorTotal *= 2f;
                    if (valorTotal > 0)
                    {
                        defensor.queimaduras.Add(new QueimaduraInstance 
                        { 
                            danoAtual = valorTotal, 
                            proximoTick = Time.time + 0.5f 
                        });
                    }
                    break;

                case TipoElemento.Veneno:
                    if (valorTotal > 0)
                    {
                        defensor.venenos.Add(new VenenoInstance
                        {
                            danoPorTick = valorTotal,
                            proximoTick = Time.time + 1.0f,
                            tempoFim = Time.time + 5.0f 
                        });
                    }
                    break;
            }
        }

        foreach (var ef in item.efeitos)
        {
            if (ef.modo != ModoEfeito.Ativo) continue;

            CombatenteRuntime alvo = (ef.alvo == AlvoEfeito.Usuario) ? atacante : defensor;

            switch (ef.tipoEfeito)
            {
                // --- NOVO EFEITO: Aumentar Duração de Debuffs Atuais ---
                case TipoEfeitoItem.AumentarDuracaoDebuffs:
                    if (ef.valor > 0)
                    {
                        if (alvo.tempoAtordoado > 0) alvo.tempoAtordoado += ef.valor;
                        if (alvo.tempoMolhado > 0) alvo.tempoMolhado += ef.valor;
                        if (alvo.tempoVulneravel > 0) alvo.tempoVulneravel += ef.valor;
                        if (alvo.tempoInflamavel > 0) alvo.tempoInflamavel += ef.valor;
                        if (alvo.tempoCorrosao > 0) alvo.tempoCorrosao += ef.valor;
                        
                        foreach(var v in alvo.venenos)
                        {
                            if (v.tempoFim > Time.time) v.tempoFim += ef.valor;
                        }

                        foreach(var q in alvo.queimaduras)
                        {
                            if (q.danoAtual > 0) q.danoAtual += ef.valor;
                        }

                        Debug.Log($"{alvo.nome} teve seus debuffs estendidos em {ef.valor}s!");
                    }
                    break;
                // -------------------------------------------------------

                case TipoEfeitoItem.Atordoar:
                    if (ef.alvo == AlvoEfeito.Oponente) defensor.tempoAtordoado += ef.duracao;
                    break;

                case TipoEfeitoItem.Molhar:
                    if (ef.alvo == AlvoEfeito.Oponente) defensor.tempoMolhado += ef.duracao;
                    break;

                case TipoEfeitoItem.Vulneravel:
                    if (ef.alvo == AlvoEfeito.Oponente) defensor.tempoVulneravel += ef.duracao;
                    break;

                case TipoEfeitoItem.Inflamavel:
                    if (ef.alvo == AlvoEfeito.Oponente) defensor.tempoInflamavel += ef.duracao;
                    break;

                case TipoEfeitoItem.Corrosao:
                    if (ef.alvo == AlvoEfeito.Oponente) 
                    {
                        defensor.tempoCorrosao += ef.duracao;
                        Debug.Log($"{defensor.nome} sofreu CORROSÃO por {ef.duracao}s!");
                    }
                    break;

                case TipoEfeitoItem.Invulneravel:
                    CombatenteRuntime alvoInvul = (ef.alvo == AlvoEfeito.Usuario) ? atacante : defensor;
                    alvoInvul.tempoInvulneravel += ef.duracao;
                    Debug.Log($"{alvoInvul.nome} ficou INVULNERÁVEL por {ef.duracao}s!");
                    break;

                case TipoEfeitoItem.AumentarDano:
                    if (ef.duracao <= 0)
                    {
                        if (ef.alvo == AlvoEfeito.Usuario)
                        {
                            atacante.buffsPermanentes.Add(new BuffRuntime
                            {
                                mascaraAlvo = ef.parametroExtra,
                                elemento = ef.elementoAlvo,
                                valor = ef.valor
                            });
                            Debug.Log($"{atacante.nome} ganhou +{ef.valor} de dano {ef.elementoAlvo} (Acumulado)!");
                        }
                    }
                    break;

                case TipoEfeitoItem.RoubarVida:
                    if (danoVidaTotalCausado > 0)
                    {
                        float curaRoubo = danoVidaTotalCausado * ef.valor; 
                        if (curaRoubo > 0)
                        {
                            atacante.vidaAtual = Mathf.Min(atacante.vidaMax, atacante.vidaAtual + curaRoubo);
                            Debug.Log($"{atacante.nome} ROUBOU {curaRoubo} de vida!");
                        }
                    }
                    break;

                case TipoEfeitoItem.ReduzirCooldownTipo:
                    ModificarCooldownAtual(alvo, ef.parametroExtra, -ef.valor); 
                    break;

                case TipoEfeitoItem.ReduzirCooldownAdjacente:
                    ReduzirCooldownVizinho(atacante, slotIndex, -ef.valor, ef.parametroExtra == 0); 
                    break;

                case TipoEfeitoItem.UsarAdjacente:
                    ForcarAtivacaoVizinho(atacante, slotIndex, ef.parametroExtra == 1);
                    break;
            }
        }
    }

    float AplicarDanoBruto(CombatenteRuntime alvo, float dano, bool ignorarArmadura)
    {
        if (dano <= 0) return 0f;

        if (alvo.tempoInvulneravel > 0)
        {
            Debug.Log($"Dano bloqueado! {alvo.nome} está invulnerável.");
            return 0f;
        }

        float danoRestante = dano;

        if (!ignorarArmadura && alvo.armaduraAtual > 0)
        {
            float armaduraEfetiva = alvo.armaduraAtual;
            if (alvo.tempoCorrosao > 0) armaduraEfetiva /= 2f; 

            float absorvido = Mathf.Min(armaduraEfetiva, danoRestante);
            alvo.armaduraAtual -= absorvido;
            danoRestante -= absorvido;
        }

        float danoVidaAplicado = 0f;
        if (danoRestante > 0)
        {
            float vidaAnterior = alvo.vidaAtual;
            alvo.vidaAtual = Mathf.Max(0, alvo.vidaAtual - danoRestante);
            danoVidaAplicado = vidaAnterior - alvo.vidaAtual;
        }
        return danoVidaAplicado;
    }

    void ModificarCooldownAtual(CombatenteRuntime c, int tipoItemMask, float deltaTempo)
    {
        for (int i = 0; i < c.slotsItens.Length; i++)
        {
            var it = c.slotsItens[i];
            if (it == null) continue;
            if (it.PossuiTipo(tipoItemMask))
            {
                if (c.ultimoUso.ContainsKey(i)) c.ultimoUso[i] += deltaTempo; 
            }
        }
    }

    void ReduzirCooldownVizinho(CombatenteRuntime c, int indexOrigem, float deltaTempo, bool apenasUm)
    {
        if (indexOrigem > 0 && c.slotsItens[indexOrigem - 1] != null)
        {
             if (c.ultimoUso.ContainsKey(indexOrigem - 1)) c.ultimoUso[indexOrigem - 1] += deltaTempo;
        }
        if (indexOrigem < c.slotsItens.Length - 1 && c.slotsItens[indexOrigem + 1] != null)
        {
             if (c.ultimoUso.ContainsKey(indexOrigem + 1)) c.ultimoUso[indexOrigem + 1] += deltaTempo;
        }
    }

    void ForcarAtivacaoVizinho(CombatenteRuntime c, int indexOrigem, bool apenasUm)
    {
        List<int> vizinhosValidos = new List<int>();

        if (indexOrigem > 0 && c.slotsItens[indexOrigem - 1] != null)
            vizinhosValidos.Add(indexOrigem - 1);

        if (indexOrigem < c.slotsItens.Length - 1 && c.slotsItens[indexOrigem + 1] != null)
            vizinhosValidos.Add(indexOrigem + 1);

        if (vizinhosValidos.Count == 0) return;

        if (apenasUm)
        {
            int escolhido = vizinhosValidos[Random.Range(0, vizinhosValidos.Count)];
            c.ultimoUso[escolhido] = -9999f;
        }
        else
        {
            foreach (var idx in vizinhosValidos)
            {
                c.ultimoUso[idx] = -9999f;
            }
        }
    }

    bool VerificarMorte()
    {
        if (heroi.vidaAtual <= 0)
        {
            emCombate = false;
            OnCombateFinalizado?.Invoke(ResultadoCombate.MonstroVenceu);
            return true;
        }
        if (monstro.vidaAtual <= 0)
        {
            emCombate = false;
            OnCombateFinalizado?.Invoke(ResultadoCombate.HeroiVenceu);
            return true;
        }
        return false;
    }

    void DispararVidaAtualizada()
    {
        OnVidaAtualizada?.Invoke(
            HeroiVidaAtual, HeroiArmaduraAtual, HeroiVidaMax, 
            MonstroVidaAtual, MonstroArmaduraAtual, MonstroVidaMax
        );
    }
}

public enum ResultadoCombate 
{    
    HeroiVenceu, 
    MonstroVenceu 
}
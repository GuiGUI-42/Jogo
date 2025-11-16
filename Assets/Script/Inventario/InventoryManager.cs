using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    static bool _quitting = false;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<InventoryManager>();
                if (_instance == null)
                    _instance = UnityEngine.Object.FindAnyObjectByType<InventoryManager>();

                if (_instance == null)
                {
                    if (_quitting)
                    {
                        // Evita criar objetos no fechamento do Play Mode
                        return null;
                    }
                    var go = new GameObject("InventoryManager");
                    _instance = go.AddComponent<InventoryManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    static InventoryManager _instance;

    [Serializable]
    public class Entry
    {
        public ScriptableObject asset; // pode ser Item, ItemCombate, etc.
        public int quantidade;
    }

    [Header("Inventário do Jogador")]
    public List<Entry> itens = new();

    public event Action OnInventoryChanged;

    // Método público para disparar atualização manual do inventário (ex: reorder externo)
    public void RaiseInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void Add(Item item, int quantidade = 1)
    {
        if (item == null) return;
        AddAsset(item, quantidade);
    }

    public void AddAsset(ScriptableObject asset, int quantidade = 1)
    {
        if (!asset || quantidade <= 0) return;
        var e = itens.Find(x => x.asset == asset);
        if (e == null)
        {
            e = new Entry { asset = asset, quantidade = quantidade };
            itens.Add(e);
        }
        else
        {
            e.quantidade += quantidade;
        }
        var nome = ObterNome(asset);
        Debug.Log($"[Inventory] Adicionado: {nome} x{quantidade}. Total agora: {e.quantidade}");
        OnInventoryChanged?.Invoke();
    }

    public int GetQuantidade(ScriptableObject asset)
    {
        var e = itens.Find(x => x.asset == asset);
        return e != null ? e.quantidade : 0;
    }

    public bool RemoveAsset(ScriptableObject asset, int quantidade = 1)
    {
        if (!asset || quantidade <= 0) return false;
        var e = itens.Find(x => x.asset == asset);
        if (e == null) return false;
        e.quantidade -= quantidade;
        if (e.quantidade <= 0)
        {
            itens.Remove(e);
        }
        var nome = ObterNome(asset);
        Debug.Log($"[Inventory] Removido: {nome} x{quantidade}. Restante: {GetQuantidade(asset)}");
        OnInventoryChanged?.Invoke();
        return true;
    }

    string ObterNome(ScriptableObject asset)
    {
        if (!asset) return "(null)";
        // tenta campo nome ou nomeItem
        var t = asset.GetType();
        var f = t.GetField("nome", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(string)) return f.GetValue(asset) as string ?? asset.name;
        f = t.GetField("nomeItem", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(string)) return f.GetValue(asset) as string ?? asset.name;
        return asset.name;
    }

    void OnApplicationQuit()
    {
        _quitting = true;
    }
}

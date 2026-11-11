using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemInfo
{
    public string name;
    public string description;
    public Sprite icon;
    public Sprite[] examineFrames;

    public ItemInfo(string name, string description, Sprite icon, Sprite[] examineFrames)
    {
        this.name = name;
        this.description = description;
        this.icon = icon;
        this.examineFrames = examineFrames;
    }

    // Frame 0 = a imagem principal; os seguintes são os frames extra (ex: o verso)
    public int FrameCount => 1 + (examineFrames != null ? examineFrames.Length : 0);

    public Sprite GetFrame(int index)
    {
        if (index <= 0) return icon;

        int i = index - 1;
        return (examineFrames != null && i < examineFrames.Length) ? examineFrames[i] : icon;
    }
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public event Action<CharacterType, ItemInfo> ItemAdded;

    private void Awake()
    {
        Instance = this;

        // Nova partida: começa com o inventário vazio
        GameSession.ResetItems();
    }

    public IReadOnlyList<ItemInfo> GetItems(CharacterType character)
    {
        return GameSession.GetItems(character);
    }

    public void Add(CharacterType character, ItemInfo item)
    {
        GameSession.AddItem(character, item);
        ItemAdded?.Invoke(character, item);
    }

    public bool Has(CharacterType character, string itemName)
    {
        return GameSession.HasItem(character, itemName);
    }
}
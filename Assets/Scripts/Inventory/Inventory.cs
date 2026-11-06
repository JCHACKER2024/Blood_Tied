using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemInfo
{
    public string name;
    public string description;
    public Sprite icon;

    public ItemInfo(string name, string description, Sprite icon)
    {
        this.name = name;
        this.description = description;
        this.icon = icon;
    }
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    private readonly Dictionary<CharacterType, List<ItemInfo>> items =
        new Dictionary<CharacterType, List<ItemInfo>>();

    public event Action<CharacterType, ItemInfo> ItemAdded;

    private void Awake()
    {
        Instance = this;
        items[CharacterType.Axel] = new List<ItemInfo>();
        items[CharacterType.Mike] = new List<ItemInfo>();
    }

    public IReadOnlyList<ItemInfo> GetItems(CharacterType character)
    {
        return items[character];
    }

    public void Add(CharacterType character, ItemInfo item)
    {
        items[character].Add(item);
        ItemAdded?.Invoke(character, item);
    }

    public bool Has(CharacterType character, string itemName)
    {
        return items[character].Exists(i => i.name == itemName);
    }
}
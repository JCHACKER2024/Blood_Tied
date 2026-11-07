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
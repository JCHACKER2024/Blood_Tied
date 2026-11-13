using System.Collections.Generic;
using UnityEngine;

public enum GameMode { Solo, Host, Client }

public static class GameSession
{
    public static CharacterType? SelectedCharacter { get; private set; }
    public static GameMode Mode { get; private set; } = GameMode.Solo;

    private static readonly Dictionary<CharacterType, List<ItemInfo>> items =
        new Dictionary<CharacterType, List<ItemInfo>>
        {
            { CharacterType.Axel, new List<ItemInfo>() },
            { CharacterType.Mike, new List<ItemInfo>() }
        };

    // Regista quem escondeu o item
    private static readonly Dictionary<string, CharacterType> hiddenItemsBy = new Dictionary<string, CharacterType>();

    // Evita spam/loops de tentar esconder várias vezes o mesmo item
    private static readonly HashSet<string> processedHideAttempts = new HashSet<string>();

    public static void Select(CharacterType character) => SelectedCharacter = character;
    public static void SetMode(GameMode mode) => Mode = mode;
    public static void Clear() => SelectedCharacter = null;

    public static IReadOnlyList<ItemInfo> GetItems(CharacterType character) => items[character];

    public static void AddItem(CharacterType character, ItemInfo item)
    {
        items[character].Add(item);
    }

    public static bool HasItem(CharacterType character, string itemName)
    {
        return items[character].Exists(i => i.name == itemName);
    }

    public static void HideItem(string itemName, CharacterType hider)
    {
        hiddenItemsBy[itemName] = hider;
        processedHideAttempts.Add(itemName);
    }

    public static bool IsHidden(string itemName)
    {
        return hiddenItemsBy.ContainsKey(itemName) && !processedHideAttempts.Contains(itemName + "_exposed");
    }

    // Regista que o item foi visto a ser escondido (ficando exposto no cenário)
    public static void ExposeItem(string itemName)
    {
        processedHideAttempts.Add(itemName + "_exposed");
    }

    public static bool IsExposed(string itemName)
    {
        return processedHideAttempts.Contains(itemName + "_exposed");
    }

    public static bool HasBeenProcessed(string itemName)
    {
        return processedHideAttempts.Contains(itemName);
    }

    public static void ResetItems()
    {
        foreach (List<ItemInfo> list in items.Values)
            list.Clear();

        hiddenItemsBy.Clear();
        processedHideAttempts.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        SelectedCharacter = null;
        Mode = GameMode.Solo;
        ResetItems();
    }
}
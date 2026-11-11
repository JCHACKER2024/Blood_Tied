using System.Collections.Generic;
using UnityEngine;

public enum GameMode { Solo, Host, Client }

public static class GameSession
{
    // null = ainda não escolheu (ex: abriste a cena Game diretamente no editor)
    public static CharacterType? SelectedCharacter { get; private set; }

    // Solo por defeito. Host e Client serão usados no co-op.
    public static GameMode Mode { get; private set; } = GameMode.Solo;

    // Itens apanhados: sobrevivem às mudanças de cena (ex: Game -> Interrogatorio)
    private static readonly Dictionary<CharacterType, List<ItemInfo>> items =
        new Dictionary<CharacterType, List<ItemInfo>>
        {
            { CharacterType.Axel, new List<ItemInfo>() },
            { CharacterType.Mike, new List<ItemInfo>() }
        };

    // Itens que o Mike escondeu (o Axel perde o acesso)
    private static readonly HashSet<string> hiddenItems = new HashSet<string>();

    public static IReadOnlyCollection<string> HiddenItems => hiddenItems;

    public static void Select(CharacterType character)
    {
        SelectedCharacter = character;
    }

    public static void SetMode(GameMode mode)
    {
        Mode = mode;
    }

    public static void Clear()
    {
        SelectedCharacter = null;
    }

    public static IReadOnlyList<ItemInfo> GetItems(CharacterType character)
    {
        return items[character];
    }

    public static void AddItem(CharacterType character, ItemInfo item)
    {
        items[character].Add(item);
    }

    public static bool HasItem(CharacterType character, string itemName)
    {
        return items[character].Exists(i => i.name == itemName);
    }

    public static void HideItem(string itemName)
    {
        hiddenItems.Add(itemName);
    }

    public static bool IsHidden(string itemName)
    {
        return hiddenItems.Contains(itemName);
    }

    // Nova partida: limpa os itens apanhados e os escondidos
    public static void ResetItems()
    {
        foreach (List<ItemInfo> list in items.Values)
            list.Clear();

        hiddenItems.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        SelectedCharacter = null;
        Mode = GameMode.Solo;
        ResetItems();
    }
}
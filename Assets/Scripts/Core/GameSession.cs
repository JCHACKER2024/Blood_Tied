using System.Collections.Generic;
using UnityEngine;

public enum GameMode { Solo, Host, Client }
public enum GameOutcome { AxelWins, MikeWins, BothJailed }

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

    // Itens recolhidos (apanhados ou fotografados) e os pontos que dão a cada personagem.
    // Cada item conta uma única vez, mesmo que os dois o recolham.
    private static readonly Dictionary<string, (int axel, int mike)> collectedPoints =
        new Dictionary<string, (int axel, int mike)>();

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

    // ---------- Pontos ----------

    // Chamado quando um item é recolhido. Os pontos entram na conta dos dois, uma única vez por item.
    public static void RegisterCollected(string itemName, int axelPoints, int mikePoints)
    {
        if (collectedPoints.ContainsKey(itemName)) return;
        collectedPoints[itemName] = (axelPoints, mikePoints);
    }

    public static int GetScore(CharacterType character)
    {
        int total = 0;

        foreach (var entry in collectedPoints.Values)
            total += character == CharacterType.Axel ? entry.axel : entry.mike;

        return total;
    }

    // Vence quem tiver mais pontos, desde que sejam positivos.
    // Empate, ou ninguém com pontos positivos: vão os dois presos.
    public static GameOutcome GetOutcome()
    {
        int axel = GetScore(CharacterType.Axel);
        int mike = GetScore(CharacterType.Mike);

        if (axel > mike && axel > 0) return GameOutcome.AxelWins;
        if (mike > axel && mike > 0) return GameOutcome.MikeWins;
        return GameOutcome.BothJailed;
    }

    public static void ResetItems()
    {
        foreach (List<ItemInfo> list in items.Values)
            list.Clear();

        hiddenItemsBy.Clear();
        processedHideAttempts.Clear();
        collectedPoints.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        SelectedCharacter = null;
        Mode = GameMode.Solo;
        ResetItems();
    }
}
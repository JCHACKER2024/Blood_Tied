using UnityEngine;

public static class GameSession
{
    // null = ainda não escolheu (ex: abriste a cena Game diretamente no editor)
    public static CharacterType? SelectedCharacter { get; private set; }

    public static void Select(CharacterType character)
    {
        SelectedCharacter = character;
    }

    public static void Clear()
    {
        SelectedCharacter = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        SelectedCharacter = null;
    }
}
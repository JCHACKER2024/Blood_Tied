using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelect : MonoBehaviour
{
    [Tooltip("Cena seguinte. Mais tarde: a cutscene.")]
    [SerializeField] private string nextSceneName = "Game";
    [SerializeField] private string menuSceneName = "MainMenu";
    [Tooltip("Espera (segundos) para o som do clique acabar antes de mudar de cena.")]
    [SerializeField] private float exitDelay = 0.25f;

    private bool leaving;

    public void SelectAxel()
    {
        Choose(CharacterType.Axel);
    }

    public void SelectMike()
    {
        Choose(CharacterType.Mike);
    }

    public void Back()
    {
        if (leaving) return;
        StartCoroutine(LoadRoutine(menuSceneName));
    }

    private void Choose(CharacterType character)
    {
        if (leaving) return;

        GameSession.Select(character);
        StartCoroutine(LoadRoutine(nextSceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        leaving = true;
        yield return new WaitForSecondsRealtime(exitDelay);

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"CharacterSelect: a cena '{sceneName}' não existe nas Build Settings. Confirma o nome e adiciona-a em File > Build Profiles.", this);
            leaving = false;
            yield break;
        }

        SceneManager.LoadScene(sceneName);
    }
}
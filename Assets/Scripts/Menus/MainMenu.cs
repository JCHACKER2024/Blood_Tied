using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Cena seguinte ao Jogar sozinho (a seleção de personagem).")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject catalogPanel;
    [Tooltip("Tempo de espera (segundos) para o som do clique acabar antes de mudar de cena.")]
    [SerializeField] private float exitDelay = 0.25f;

    private bool leaving;

    private void Start()
    {
        Time.timeScale = 1f;
        ShowMain();
    }

    // Botão Play: abre o painel com os modos de jogo
    public void Play()
    {
        HideAll();
        SetActive(modePanel, true);
    }

    // Botão "Jogar sozinho"
    public void PlaySolo()
    {
        if (leaving) return;

        GameSession.SetMode(GameMode.Solo);
        StartCoroutine(PlayRoutine());
    }

    public void OpenOptions()
    {
        HideAll();
        SetActive(optionsPanel, true);
    }

    public void OpenCatalog()
    {
        HideAll();
        SetActive(catalogPanel, true);
    }

    public void ShowMain()
    {
        HideAll();
        SetActive(mainPanel, true);
    }

    public void Quit()
    {
        if (leaving) return;
        StartCoroutine(QuitRoutine());
    }

    private void HideAll()
    {
        SetActive(mainPanel, false);
        SetActive(modePanel, false);
        SetActive(optionsPanel, false);
        SetActive(catalogPanel, false);
    }

    private static void SetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    private IEnumerator PlayRoutine()
    {
        leaving = true;
        yield return new WaitForSecondsRealtime(exitDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator QuitRoutine()
    {
        leaving = true;
        yield return new WaitForSecondsRealtime(exitDelay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
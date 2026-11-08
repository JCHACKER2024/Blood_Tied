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
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (modePanel != null) modePanel.SetActive(true);
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
        if (mainPanel != null) mainPanel.SetActive(false);
        if (modePanel != null) modePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void ShowMain()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (modePanel != null) modePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void Quit()
    {
        if (leaving) return;
        StartCoroutine(QuitRoutine());
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
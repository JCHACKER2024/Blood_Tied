using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [Tooltip("Tempo de espera (segundos) para o som do clique acabar antes de mudar de cena.")]
    [SerializeField] private float exitDelay = 0.25f;

    private bool leaving;

    private void Start()
    {
        Time.timeScale = 1f;
        ShowMain();
    }

    public void Play()
    {
        if (leaving) return;
        StartCoroutine(PlayRoutine());
    }

    public void OpenOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void ShowMain()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
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
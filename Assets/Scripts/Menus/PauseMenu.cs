using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;

    [Header("Cena")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private readonly Dictionary<PlayerController, bool> previousControl = new Dictionary<PlayerController, bool>();
    private bool uiWasOpen;
    private bool leaving;

    private bool SoloOnly => GameSession.Mode == GameMode.Solo;

    private void Awake()
    {
        IsPaused = false;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(BackToMenu);
    }

    private void OnDestroy()
    {
        if (IsPaused) Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
        if (!SoloOnly || leaving) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (IsPaused)
            Resume();
        else if (CanOpen())
            Pause();
    }

    private void LateUpdate()
    {
        uiWasOpen = InventoryUI.IsOpen;
    }

    private bool CanOpen()
    {
        return !InventoryUI.IsOpen && !uiWasOpen && !ScreenFader.Busy;
    }

    public void Pause()
    {
        IsPaused = true;

        previousControl.Clear();
        foreach (PlayerController p in FindObjectsByType<PlayerController>())
        {
            previousControl[p] = p.IsControlled;
            p.SetControlled(false);
        }

        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
    }

    public void Resume()
    {
        foreach (KeyValuePair<PlayerController, bool> entry in previousControl)
        {
            if (entry.Key != null) entry.Key.SetControlled(entry.Value);
        }
        previousControl.Clear();

        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void BackToMenu()
    {
        if (leaving) return;

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogWarning($"PauseMenu: a cena '{menuSceneName}' não existe nas Build Settings.", this);
            return;
        }

        leaving = true;

        StartCoroutine(ScreenFader.Play(() =>
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene(menuSceneName);
        }));
    }
}

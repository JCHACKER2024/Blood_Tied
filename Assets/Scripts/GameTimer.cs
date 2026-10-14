using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    [Header("Tempo")]
    [Tooltip("Duração em segundos (120 = 2 minutos).")]
    [SerializeField] private float durationSeconds = 120f;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;

    [Header("Fim do tempo")]
    [Tooltip("Nome exato da cena do menu principal.")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private float remaining;
    private bool finished;

    public float Remaining => remaining;
    public event Action TimeUp;

    private void Start()
    {
        remaining = durationSeconds;
        UpdateUI();
    }

    private void Update()
    {
        if (finished) return;

        remaining -= Time.deltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            UpdateUI();
            Finish();
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timeText == null) return;

        int total = Mathf.CeilToInt(remaining);
        timeText.text = $"{total / 60:00}:{total % 60:00}";
    }

    private void Finish()
    {
        finished = true;

        // Os players deixam de se mexer
        foreach (PlayerController p in FindObjectsByType<PlayerController>())
            p.SetControlled(false);

        TimeUp?.Invoke();

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogWarning($"GameTimer: a cena '{menuSceneName}' não existe nas Build Settings. Confirma o nome e adiciona-a em File > Build Profiles.", this);
            return;
        }

        StartCoroutine(ScreenFader.Play(() => SceneManager.LoadScene(menuSceneName)));
    }
}
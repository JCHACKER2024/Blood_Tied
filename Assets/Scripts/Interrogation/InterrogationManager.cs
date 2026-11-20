using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class InterrogationQuestion
{
    [TextArea] public string question;
    [Tooltip("Item Name dos itens que respondem bem (exatamente como escrito no Pickup Item).")]
    public string[] goodItems;
    [TextArea] public string goodReply;
    [TextArea] public string badReply;
}

[Serializable]
public class InterrogationEnding
{
    public string title;
    [TextArea] public string text;
}

public class InterrogationManager : MonoBehaviour
{
    [Header("Conteúdo (editável no Inspector)")]
    [SerializeField] private InterrogationQuestion[] questions;
    [Tooltip("Usado se abrires esta cena diretamente no editor.")]
    [SerializeField] private CharacterType fallbackCharacter = CharacterType.Axel;

    [Header("Finais (decididos pelos pontos)")]
    [SerializeField] private InterrogationEnding axelWinsEnding;
    [SerializeField] private InterrogationEnding mikeWinsEnding;
    [Tooltip("Empate, ou nenhum dos dois com pontos positivos.")]
    [SerializeField] private InterrogationEnding bothJailedEnding;

    [Header("Tempo para responder")]
    [Tooltip("Segundos por pergunta. 0 = sem limite.")]
    [SerializeField] private float secondsPerQuestion = 15f;
    [SerializeField] private Slider timerBar;
    [TextArea]
    [SerializeField] private string timeoutReply = "Demoraste demasiado a responder...";

    [Header("UI - Pergunta")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text replyText;
    [Tooltip("Mostra os pontos do jogador que está a jogar.")]
    [SerializeField] private TMP_Text progressText;
    [Tooltip("{0} = pontos do jogador. Ex: 'PONTOS {0}'")]
    [SerializeField] private string pointsFormat = "Pontos: {0}";
    [SerializeField] private Transform answersParent;
    [SerializeField] private AnswerSlot answerSlotPrefab;
    [SerializeField] private Button noAnswerButton;
    [SerializeField] private Button continueButton;

    [Header("UI - Final")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingBodyText;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuSceneName = "MainMenu";

    private readonly List<AnswerSlot> slots = new List<AnswerSlot>();
    private CharacterType character;
    private int index;
    private bool answered;
    private float timeLeft;
    private bool timerRunning;

    private void Start()
    {
        Time.timeScale = 1f;

        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("InterrogationManager: não há perguntas definidas.", this);
            return;
        }

        character = GameSession.SelectedCharacter ?? fallbackCharacter;

        foreach (ItemInfo item in GameSession.GetItems(character))
        {
            AnswerSlot slot = Instantiate(answerSlotPrefab, answersParent);
            slot.Setup(item, Answer);
            slots.Add(slot);
        }

        noAnswerButton.onClick.AddListener(() => Answer(null));
        continueButton.onClick.AddListener(Next);
        menuButton.onClick.AddListener(BackToMenu);

        endingPanel.SetActive(false);
        ShowQuestion();
    }

    private void Update()
    {
        if (!timerRunning || answered) return;

        timeLeft -= Time.deltaTime;

        if (timerBar != null)
            timerBar.value = Mathf.Clamp01(timeLeft / secondsPerQuestion);

        if (timeLeft <= 0f)
            Resolve(null, true);
    }

    private void ShowQuestion()
    {
        answered = false;

        InterrogationQuestion q = questions[index];
        questionText.text = q.question;
        replyText.text = "";

        if (progressText != null)
            progressText.text = string.Format(pointsFormat, GameSession.GetScore(character));

        timerRunning = secondsPerQuestion > 0f;
        timeLeft = secondsPerQuestion;

        if (timerBar != null)
        {
            timerBar.gameObject.SetActive(timerRunning);
            timerBar.value = 1f;
        }

        SetAnswersInteractable(true);
        continueButton.gameObject.SetActive(false);
    }

    private void Answer(ItemInfo item)
    {
        Resolve(item, false);
    }

    private void Resolve(ItemInfo item, bool timedOut)
    {
        if (answered) return;
        answered = true;
        timerRunning = false;

        InterrogationQuestion q = questions[index];
        bool good = !timedOut && item != null && IsGood(q, item.name);

        // As respostas só mudam a reação do interrogador; o final depende dos pontos.
        if (timedOut)
            replyText.text = timeoutReply;
        else
            replyText.text = good ? q.goodReply : q.badReply;

        SetAnswersInteractable(false);
        continueButton.gameObject.SetActive(true);
    }

    private void Next()
    {
        index++;

        if (index >= questions.Length)
            ShowEnding();
        else
            ShowQuestion();
    }

    private void ShowEnding()
    {
        timerRunning = false;
        if (timerBar != null) timerBar.gameObject.SetActive(false);

        GameOutcome outcome = GameSession.GetOutcome();
        int axelScore = GameSession.GetScore(CharacterType.Axel);
        int mikeScore = GameSession.GetScore(CharacterType.Mike);

        InterrogationEnding ending;
        string defaultTitle;

        switch (outcome)
        {
            case GameOutcome.AxelWins:
                ending = axelWinsEnding;
                defaultTitle = "O Axel vence";
                break;
            case GameOutcome.MikeWins:
                ending = mikeWinsEnding;
                defaultTitle = "O Mike vence";
                break;
            default:
                ending = bothJailedEnding;
                defaultTitle = "Os dois são presos";
                break;
        }

        string title = (ending != null && !string.IsNullOrEmpty(ending.title)) ? ending.title : defaultTitle;
        string body = ending != null ? ending.text : "";

        endingPanel.SetActive(true);
        endingTitleText.text = title;
        endingBodyText.text = body + $"\n\nPontos - Axel: {axelScore} | Mike: {mikeScore}";
    }

    private static bool IsGood(InterrogationQuestion q, string itemName)
    {
        if (q.goodItems == null) return false;

        foreach (string g in q.goodItems)
        {
            if (string.Equals(g.Trim(), itemName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void SetAnswersInteractable(bool value)
    {
        foreach (AnswerSlot s in slots) s.SetInteractable(value);
        noAnswerButton.interactable = value;
    }

    private void BackToMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogWarning($"InterrogationManager: a cena '{menuSceneName}' não existe nas Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}
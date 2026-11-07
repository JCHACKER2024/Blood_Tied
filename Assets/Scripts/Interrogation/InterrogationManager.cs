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
    [Tooltip("Respostas certas necessárias para este final. Vence o maior valor que for atingido.")]
    public int minCorrect;
}

public class InterrogationManager : MonoBehaviour
{
    [Header("Conteúdo (editável no Inspector)")]
    [SerializeField] private InterrogationQuestion[] questions;
    [SerializeField] private InterrogationEnding[] endings;
    [Tooltip("Usado se abrires esta cena diretamente no editor.")]
    [SerializeField] private CharacterType fallbackCharacter = CharacterType.Axel;

    [Header("UI - Pergunta")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text replyText;
    [SerializeField] private TMP_Text progressText;
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
    private int index;
    private int correct;
    private bool answered;

    private void Start()
    {
        Time.timeScale = 1f;

        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("InterrogationManager: não há perguntas definidas.", this);
            return;
        }

        CharacterType character = GameSession.SelectedCharacter ?? fallbackCharacter;

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

    private void ShowQuestion()
    {
        answered = false;

        InterrogationQuestion q = questions[index];
        questionText.text = q.question;
        replyText.text = "";

        if (progressText != null)
            progressText.text = $"Pergunta {index + 1}/{questions.Length}";

        SetAnswersInteractable(true);
        continueButton.gameObject.SetActive(false);
    }

    private void Answer(ItemInfo item)
    {
        if (answered) return;
        answered = true;

        InterrogationQuestion q = questions[index];
        bool good = item != null && IsGood(q, item.name);
        if (good) correct++;

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
        InterrogationEnding best = null;

        if (endings != null)
        {
            foreach (InterrogationEnding e in endings)
            {
                if (correct >= e.minCorrect && (best == null || e.minCorrect > best.minCorrect))
                    best = e;
            }
        }

        endingPanel.SetActive(true);
        endingTitleText.text = best != null ? best.title : "Fim";
        endingBodyText.text = (best != null ? best.text : "") +
                              $"\n\nRespostas certas: {correct}/{questions.Length}";
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
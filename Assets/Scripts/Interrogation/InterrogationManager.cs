using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
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
    [Header("Perguntas (uma lista por personagem)")]
    [SerializeField] private InterrogationQuestion[] axelQuestions;
    [SerializeField] private InterrogationQuestion[] mikeQuestions;
    [Tooltip("Usado se abrires esta cena diretamente no editor.")]
    [SerializeField] private CharacterType fallbackCharacter = CharacterType.Axel;

    [Header("Regras")]
    [Tooltip("Ligado: um item usado numa resposta errada também fica gasto. Desligado: continua disponível.")]
    [SerializeField] private bool loseItemOnWrongAnswer = false;

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

    [Header("UI - À espera do outro jogador (opcional)")]
    [Tooltip("Painel mostrado quando acabas e o outro ainda não. Se ficar vazio, o aviso aparece no texto da pergunta.")]
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private TMP_Text waitingText;
    [Tooltip("{0} = nome do outro personagem.")]
    [SerializeField] private string waitingFormat = "Waiting for {0} to finish...";

    [Header("UI - Final")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingBodyText;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuSceneName = "MainMenu";

    private readonly List<AnswerSlot> slots = new List<AnswerSlot>();
    private readonly Dictionary<ItemInfo, AnswerSlot> slotByItem = new Dictionary<ItemInfo, AnswerSlot>();
    private readonly List<string> myAwarded = new List<string>();

    private InterrogationQuestion[] questions;
    private CharacterType character;
    private int index;
    private bool answered;
    private float timeLeft;
    private bool timerRunning;
    private bool waiting;
    private bool leaving;

    private void Start()
    {
        Time.timeScale = 1f;

        character = GameSession.SelectedCharacter ?? fallbackCharacter;
        questions = character == CharacterType.Axel ? axelQuestions : mikeQuestions;

        foreach (ItemInfo item in GameSession.GetItems(character))
        {
            AnswerSlot slot = Instantiate(answerSlotPrefab, answersParent);
            slot.Setup(item, Answer);
            slots.Add(slot);
            slotByItem[item] = slot;
        }

        noAnswerButton.onClick.AddListener(() => Answer(null));
        continueButton.onClick.AddListener(Next);
        menuButton.onClick.AddListener(BackToMenu);

        endingPanel.SetActive(false);
        if (waitingPanel != null) waitingPanel.SetActive(false);

        RefreshPoints();

        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning($"InterrogationManager: não há perguntas definidas para o {character}.", this);
            FinishQuestions();
            return;
        }

        ShowQuestion();
    }

    private void Update()
    {
        // À espera do outro jogador
        if (waiting)
        {
            InterrogationNetworkSync sync = InterrogationNetworkSync.Instance;

            if (sync != null && sync.PartnerFinished)
            {
                waiting = false;
                ShowEnding();
            }
            else if (ConnectionLost())
            {
                waiting = false;
                ShowConnectionLost();
            }
            return;
        }

        if (!timerRunning || answered) return;

        timeLeft -= Time.deltaTime;

        if (timerBar != null)
            timerBar.value = Mathf.Clamp01(timeLeft / secondsPerQuestion);

        if (timeLeft <= 0f)
            Resolve(null, true);
    }

    // ---------- Perguntas ----------

    private void ShowQuestion()
    {
        answered = false;

        InterrogationQuestion q = questions[index];
        questionText.text = q.question;
        replyText.text = "";

        RefreshPoints();

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

        if (good)
        {
            // Acertou: o item dá os pontos (uma única vez) e fica gasto
            if (GameSession.AwardPoints(item.name))
                myAwarded.Add(item.name);

            ConsumeItem(item);
        }
        else if (!timedOut && item != null && loseItemOnWrongAnswer)
        {
            ConsumeItem(item);
        }

        if (timedOut)
            replyText.text = timeoutReply;
        else
            replyText.text = good ? q.goodReply : q.badReply;

        RefreshPoints();

        SetAnswersInteractable(false);
        continueButton.gameObject.SetActive(true);
    }

    private void ConsumeItem(ItemInfo item)
    {
        if (item != null && slotByItem.TryGetValue(item, out AnswerSlot slot) && slot != null)
            slot.gameObject.SetActive(false);
    }

    private void Next()
    {
        index++;

        if (index >= questions.Length)
            FinishQuestions();
        else
            ShowQuestion();
    }

    // ---------- Fim das perguntas ----------

    private void FinishQuestions()
    {
        timerRunning = false;
        if (timerBar != null) timerBar.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        SetAnswersInteractable(false);

        // Solo: não há ninguém à espera
        if (GameSession.Mode == GameMode.Solo)
        {
            ShowEnding();
            return;
        }

        InterrogationNetworkSync sync = InterrogationNetworkSync.Instance;
        if (sync == null || !sync.IsSpawned)
        {
            Debug.LogWarning("InterrogationManager: falta o InterrogationNetworkSync (com NetworkObject) na cena. A mostrar o final sem esperar.", this);
            ShowEnding();
            return;
        }

        // Avisa o outro PC e envia os itens que acertaram
        InterrogationNetworkSync.NotifyFinished(myAwarded);

        if (sync.PartnerFinished)
            ShowEnding();
        else
            ShowWaiting();
    }

    private void ShowWaiting()
    {
        waiting = true;

        string partner = character == CharacterType.Axel ? "Mike" : "Axel";
        string message = string.Format(waitingFormat, partner);

        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);
            if (waitingText != null) waitingText.text = message;
        }
        else
        {
            questionText.text = message;
            replyText.text = "";
        }
    }

    private void ShowConnectionLost()
    {
        if (waitingPanel != null) waitingPanel.SetActive(false);

        endingPanel.SetActive(true);
        endingTitleText.text = "Ligação perdida";
        endingBodyText.text = "O outro jogador saiu. Volta ao menu.";
    }

    private void ShowEnding()
    {
        timerRunning = false;
        if (timerBar != null) timerBar.gameObject.SetActive(false);
        if (waitingPanel != null) waitingPanel.SetActive(false);

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

    // ---------- Util ----------

    private void RefreshPoints()
    {
        if (progressText != null)
            progressText.text = string.Format(pointsFormat, GameSession.GetScore(character));
    }

    private static bool ConnectionLost()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return true;

        if (nm.IsServer) return nm.ConnectedClientsIds.Count < 2;
        return !nm.IsConnectedClient;
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
        foreach (AnswerSlot s in slots)
        {
            if (s != null) s.SetInteractable(value);
        }
        noAnswerButton.interactable = value;
    }

    private void BackToMenu()
    {
        if (leaving) return;

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogWarning($"InterrogationManager: a cena '{menuSceneName}' não existe nas Build Settings.", this);
            return;
        }

        // Online: fecha a ligação antes de mudar de cena
        if (GameSession.Mode != GameMode.Solo)
        {
            StartCoroutine(LeaveNetworkAndLoad());
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private IEnumerator LeaveNetworkAndLoad()
    {
        leaving = true;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();
            yield return new WaitUntil(() => nm == null || !nm.ShutdownInProgress);
        }

        GameSession.SetMode(GameMode.Solo);
        SceneManager.LoadScene(menuSceneName);
    }
}
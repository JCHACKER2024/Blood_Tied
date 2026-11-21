using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTimer : NetworkBehaviour
{
    [Header("Tempo")]
    [Tooltip("Duração em segundos (120 = 2 minutos).")]
    [SerializeField] private float durationSeconds = 120f;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;

    [Header("Fim do tempo")]
    [Tooltip("Cena para onde vai quando o tempo acaba (nome exato). Em co-op é o host que a carrega para os dois.")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [Tooltip("Ligado: cada PC fecha a ligação antes de mudar de cena (usa quando o destino é o menu).")]
    [SerializeField] private bool leaveNetworkOnFinish = false;

    // Hora (do servidor) a que o tempo acaba. 0 = ainda não definida. Só o host escreve.
    private readonly NetworkVariable<double> endServerTime = new NetworkVariable<double>(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float soloRemaining;
    private float remaining;
    private bool finished;

    private bool IsOnline => GameSession.Mode != GameMode.Solo;

    public float Remaining => remaining;
    public event Action TimeUp;

    private void Start()
    {
        remaining = durationSeconds;
        soloRemaining = durationSeconds;
        UpdateUI();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // O host define a hora de fim; o cliente recebe-a pela NetworkVariable
        if (IsServer)
            endServerTime.Value = NetworkManager.Singleton.ServerTime.Time + durationSeconds;
    }

    private void Update()
    {
        if (finished) return;

        if (IsOnline)
        {
            // Espera até a hora de fim estar definida e a rede pronta
            if (endServerTime.Value <= 0d || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;

            remaining = (float)(endServerTime.Value - NetworkManager.Singleton.ServerTime.Time);
        }
        else
        {
            soloRemaining -= Time.deltaTime;
            remaining = soloRemaining;
        }

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

        if (!IsOnline)
        {
            StartCoroutine(ScreenFader.Play(() => SceneManager.LoadScene(menuSceneName)));
            return;
        }

        if (leaveNetworkOnFinish)
        {
            // Cada PC sai da rede e muda de cena sozinho (os dois acabam ao mesmo tempo)
            StartCoroutine(LeaveNetworkAndLoad());
            return;
        }

        // Co-op normal: só o host carrega a cena, e leva o cliente atrás
        if (IsServer)
            StartCoroutine(ScreenFader.Play(() => NetworkManager.Singleton.SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single)));
    }

    private IEnumerator LeaveNetworkAndLoad()
    {
        yield return ScreenFader.Play(null);

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
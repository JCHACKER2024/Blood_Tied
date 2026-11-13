using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelect : NetworkBehaviour
{
    [Header("Cenas")]
    [SerializeField] private string nextSceneName = "Game";
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private float exitDelay = 0.25f;

    [Header("Botões de Personagem")]
    [SerializeField] private Button axelButton;
    [SerializeField] private Button mikeButton;

    [Header("Botão Pronto & Estado")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text statusText;

    // -1 = ninguém escolhido ainda | 0 = Host é Axel (Cliente é Mike) | 1 = Host é Mike (Cliente é Axel)
    private NetworkVariable<int> hostChoice = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool leaving;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        bool isHost = IsServer;

        // Só o Host pode clicar nas personagens e no botão Pronto
        if (axelButton != null) axelButton.interactable = isHost;
        if (mikeButton != null) mikeButton.interactable = isHost;

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(isHost);
            readyButton.interactable = false;
        }

        hostChoice.OnValueChanged += OnHostChoiceChanged;
        UpdateUI(hostChoice.Value);
    }

    public override void OnNetworkDespawn()
    {
        hostChoice.OnValueChanged -= OnHostChoiceChanged;
        base.OnNetworkDespawn();
    }

    public void SelectAxel()
    {
        if (!IsServer || leaving) return;
        hostChoice.Value = 0; // Host = Axel, Cliente = Mike
    }

    public void SelectMike()
    {
        if (!IsServer || leaving) return;
        hostChoice.Value = 1; // Host = Mike, Cliente = Axel
    }

    private void OnHostChoiceChanged(int oldValue, int newValue)
    {
        UpdateUI(newValue);
    }

    private void UpdateUI(int choice)
    {
        if (choice == -1)
        {
            if (statusText != null)
                statusText.text = IsServer ? "Escolhe a tua personagem" : "A aguardar escolha do Host...";
            return;
        }

        // Atribui automaticamente na GameSession local de cada PC
        CharacterType myCharacter;
        if (IsServer)
        {
            myCharacter = (choice == 0) ? CharacterType.Axel : CharacterType.Mike;
        }
        else
        {
            myCharacter = (choice == 0) ? CharacterType.Mike : CharacterType.Axel;
        }

        GameSession.Select(myCharacter);

        if (readyButton != null && IsServer)
            readyButton.interactable = true;

        if (statusText != null)
        {
            CharacterType partnerCharacter = (myCharacter == CharacterType.Axel) ? CharacterType.Mike : CharacterType.Axel;
            statusText.text = IsServer
                ? $"Tu: {myCharacter} | Parceiro: {partnerCharacter} — Carrega em PRONTO"
                : $"Tu: {myCharacter} | Host: {partnerCharacter} — A aguardar o Host iniciar...";
        }
    }

    public void ConfirmAndStartGame()
    {
        if (!IsServer || leaving || hostChoice.Value == -1) return;
        StartCoroutine(LoadNetworkSceneRoutine(nextSceneName));
    }

    public void Back()
    {
        if (leaving) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        StartCoroutine(LoadRoutine(menuSceneName));
    }

    private IEnumerator LoadNetworkSceneRoutine(string sceneName)
    {
        leaving = true;
        yield return new WaitForSecondsRealtime(exitDelay);

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"CharacterSelect: a cena '{sceneName}' não existe nas Build Settings.", this);
            leaving = false;
            yield break;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        leaving = true;
        yield return new WaitForSecondsRealtime(exitDelay);
        SceneManager.LoadScene(sceneName);
    }
}
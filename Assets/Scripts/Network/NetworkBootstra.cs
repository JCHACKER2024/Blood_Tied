using System;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkBootstrap : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Sala")]
    [Tooltip("Host: mostra o IP (só de leitura). Cliente: onde se escreve o IP.")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button startButton;

    [Header("Mensagens (opcional)")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("Desligado: sem texto no ecrã, as mensagens vão só para a Console.")]
    [SerializeField] private bool showStatusMessages = false;

    [Header("Rede")]
    [SerializeField] private ushort port = 7777;
    [Tooltip("Cena para onde vão os dois jogadores (nome exato).")]
    [SerializeField] private string nextSceneName = "CharacterSelect";

    private bool isHost;

    private void Start()
    {
        if (roomPanel != null) roomPanel.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(showStatusMessages);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // Botão "Criar sala"
    public void OpenHost()
    {
        if (!CanUseNetwork()) return;

        isHost = true;
        GameSession.SetMode(GameMode.Host);
        ShowRoomPanel();

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");

        Subscribe();

        if (!NetworkManager.Singleton.StartHost())
        {
            ipInput.text = "";
            SetStatus("Não foi possível criar a sala. Confirma que não há outra sala aberta.");
            return;
        }

        // Só o número, para copiar
        ipInput.text = GetLocalIp();
        SetStatus("Sala criada. À espera de jogador...");
    }

    // Botão "Entrar numa sala"
    public void OpenJoin()
    {
        if (!CanUseNetwork()) return;

        isHost = false;
        GameSession.SetMode(GameMode.Client);
        ShowRoomPanel();

        SetStatus("Escreve o IP do host e carrega em LIGAR (vazio = este PC).");
    }

    // Botão "Ligar" (só de quem entra)
    public void Connect()
    {
        if (!CanUseNetwork() || isHost) return;

        string ip = string.IsNullOrWhiteSpace(ipInput.text) ? "127.0.0.1" : ipInput.text.Trim();

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        Subscribe();

        if (!NetworkManager.Singleton.StartClient())
        {
            SetStatus("Não foi possível iniciar a ligação.");
            return;
        }

        connectButton.interactable = false;
        SetStatus($"A ligar a {ip}...");
    }

    // Botão "Começar" (só do host): leva os dois jogadores para a próxima cena
    public void StartGame()
    {
        if (!isHost || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SetStatus($"A cena '{nextSceneName}' não existe na Scene List (File > Build Profiles).");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }

    // Botão "Copiar" (opcional): copia o IP do campo
    public void CopyIp()
    {
        if (ipInput != null && !string.IsNullOrEmpty(ipInput.text))
            GUIUtility.systemCopyBuffer = ipInput.text;
    }

    // Botão "Voltar"
    public void Back()
    {
        Unsubscribe();

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening) nm.Shutdown();

        GameSession.SetMode(GameMode.Solo);

        ipInput.text = "";
        ipInput.readOnly = false;

        if (roomPanel != null) roomPanel.SetActive(false);
        if (modePanel != null) modePanel.SetActive(true);
    }

    private void OnClientConnected(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsHost && clientId != nm.LocalClientId)
        {
            SetStatus("Jogador ligado! Carrega em COMEÇAR.");
            startButton.interactable = true;
        }
        else if (!nm.IsHost && clientId == nm.LocalClientId)
        {
            SetStatus("Ligado! À espera que o host comece.");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsHost && clientId != nm.LocalClientId)
        {
            SetStatus("O jogador saiu. À espera de jogador...");
            startButton.interactable = false;
        }
        else if (!nm.IsHost)
        {
            SetStatus("Sem ligação. Confirma o IP e tenta outra vez.");
            connectButton.interactable = true;
        }
    }

    private void Subscribe()
    {
        Unsubscribe();

        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void Unsubscribe()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void ShowRoomPanel()
    {
        if (modePanel != null) modePanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);

        // O campo do IP aparece sempre; no host é só de leitura (dá para selecionar e copiar)
        ipInput.gameObject.SetActive(true);
        ipInput.text = "";
        ipInput.readOnly = isHost;

        connectButton.gameObject.SetActive(!isHost);
        connectButton.interactable = true;

        startButton.gameObject.SetActive(isHost);
        startButton.interactable = false;

        if (statusText != null) statusText.gameObject.SetActive(showStatusMessages);
    }

    private bool CanUseNetwork()
    {
        if (NetworkManager.Singleton != null) return true;

        Debug.LogWarning("NetworkBootstrap: não há NetworkManager na cena do menu.", this);
        return false;
    }

    private void SetStatus(string message)
    {
        Debug.Log($"NetworkBootstrap: {message}");

        if (showStatusMessages && statusText != null)
            statusText.text = message;
    }

    private static string GetLocalIp()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                if (endPoint != null) return endPoint.Address.ToString();
            }
        }
        catch (Exception)
        {
        }

        try
        {
            foreach (IPAddress addr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                    return addr.ToString();
            }
        }
        catch (Exception)
        {
        }

        return "127.0.0.1";
    }
}
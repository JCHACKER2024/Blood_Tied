using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetTestConnection : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private ushort port = 7777;

    public void StartHostButton()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClientButton()
    {
        string ip = (ipInput == null || string.IsNullOrWhiteSpace(ipInput.text))
            ? "127.0.0.1"
            : ipInput.text.Trim();

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();
    }
}
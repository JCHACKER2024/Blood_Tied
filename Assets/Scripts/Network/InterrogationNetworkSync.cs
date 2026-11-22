using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Fica num objeto vazio da cena do interrogatório (com NetworkObject).
// Quando um jogador acaba, avisa o outro e envia os itens que acertaram (para os pontos).
public class InterrogationNetworkSync : NetworkBehaviour
{
    public static InterrogationNetworkSync Instance { get; private set; }

    public static bool IsOnline =>
        GameSession.Mode != GameMode.Solo && Instance != null && Instance.IsSpawned;

    // O outro jogador já acabou as perguntas
    public bool PartnerFinished { get; private set; }

    public event Action PartnerDone;
    public event Action PartnerLeft;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        PartnerFinished = false;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

        if (Instance == this) Instance = null;
        base.OnNetworkDespawn();
    }

    // Chamado pelo InterrogationManager quando este jogador acaba.
    // awardedItems = itens que acertaram nas perguntas (os pontos do outro PC dependem disto).
    public static void NotifyFinished(IEnumerable<string> awardedItems)
    {
        if (!IsOnline) return;

        string joined = string.Join("|", awardedItems);
        Instance.FinishedServerRpc(joined);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void FinishedServerRpc(string awardedJoined, RpcParams rpcParams = default)
    {
        FinishedClientRpc(awardedJoined, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void FinishedClientRpc(string awardedJoined, ulong senderId)
    {
        // Quem enviou já aplicou localmente
        if (senderId == NetworkManager.Singleton.LocalClientId) return;

        foreach (string itemName in awardedJoined.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            GameSession.AwardPoints(itemName);

        PartnerFinished = true;
        Debug.Log($"[Rede] O outro jogador acabou o interrogatório. Pontos -> Axel: {GameSession.GetScore(CharacterType.Axel)} | Mike: {GameSession.GetScore(CharacterType.Mike)}");

        PartnerDone?.Invoke();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("[Rede] Ligação perdida durante o interrogatório.");
        PartnerLeft?.Invoke();
    }
}
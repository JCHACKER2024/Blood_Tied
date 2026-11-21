using Unity.Netcode;
using UnityEngine;

// Fica no mesmo objeto do GameNetworkSetup (o que tem o NetworkObject).
// Avisa o outro PC quando um item é recolhido ou escondido.
public class ItemNetworkSync : NetworkBehaviour
{
    public static ItemNetworkSync Instance { get; private set; }

    private static bool CanSend =>
        Instance != null && Instance.IsSpawned && GameSession.Mode != GameMode.Solo;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
        base.OnNetworkDespawn();
    }

    // ---------- Chamados pelo PickupItem ----------

    public static void NotifyCollected(string itemName, int axelPoints, int mikePoints, bool removeFromMap)
    {
        if (!CanSend) return;
        Instance.CollectedServerRpc(itemName, axelPoints, mikePoints, removeFromMap);
    }

    // caught = true: apanhado em flagrante (o item fica exposto). false: escondido com sucesso.
    public static void NotifyHidden(string itemName, CharacterType hider, bool caught)
    {
        if (!CanSend) return;
        Instance.HiddenServerRpc(itemName, hider, caught);
    }

    // ---------- Recolher ----------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CollectedServerRpc(string itemName, int axelPoints, int mikePoints, bool removeFromMap, RpcParams rpcParams = default)
    {
        CollectedClientRpc(itemName, axelPoints, mikePoints, removeFromMap, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void CollectedClientRpc(string itemName, int axelPoints, int mikePoints, bool removeFromMap, ulong senderId)
    {
        // Quem enviou já aplicou localmente
        if (senderId == NetworkManager.Singleton.LocalClientId) return;

        GameSession.RegisterCollected(itemName, axelPoints, mikePoints);

        if (removeFromMap)
        {
            PickupItem item = FindItem(itemName);
            if (item != null) Destroy(item.gameObject);
        }

        Debug.Log($"[Rede] O outro jogador recolheu {itemName} -> Axel: {GameSession.GetScore(CharacterType.Axel)} | Mike: {GameSession.GetScore(CharacterType.Mike)}");
    }

    // ---------- Esconder ----------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HiddenServerRpc(string itemName, CharacterType hider, bool caught, RpcParams rpcParams = default)
    {
        HiddenClientRpc(itemName, hider, caught, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void HiddenClientRpc(string itemName, CharacterType hider, bool caught, ulong senderId)
    {
        if (senderId == NetworkManager.Singleton.LocalClientId) return;

        GameSession.HideItem(itemName, hider);

        PickupItem item = FindItem(itemName);

        if (caught)
        {
            GameSession.ExposeItem(itemName);
            if (item != null) item.gameObject.SetActive(true);
        }
        else
        {
            if (item != null) item.gameObject.SetActive(false);
        }

        Debug.Log($"[Rede] O outro jogador {(caught ? "foi apanhado a esconder" : "escondeu")} {itemName}.");
    }

    // ---------- Util ----------

    private static PickupItem FindItem(string itemName)
    {
        foreach (PickupItem item in FindObjectsByType<PickupItem>(FindObjectsInactive.Include))
        {
            if (item != null && item.ItemName == itemName) return item;
        }
        return null;
    }
}
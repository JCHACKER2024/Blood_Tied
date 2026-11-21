using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Fica num objeto vazio da cena Game (com NetworkObject).
// O host atribui o dono de cada personagem e gera a semente dos itens.
public class GameNetworkSetup : NetworkBehaviour
{
    [Tooltip("Segundos de espera máxima até os jogadores e o cliente estarem prontos.")]
    [SerializeField] private float timeout = 10f;

    [Tooltip("O ItemSpawner da cena. Se ficar vazio, procura-o sozinho.")]
    [SerializeField] private ItemSpawner itemSpawner;

    // Semente dos itens: 0 = ainda não gerada. Só o host escreve.
    private readonly NetworkVariable<int> itemSeed = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Diagnóstico: o que cada PC acha que é
        Debug.Log($"[Rede] Este PC: personagem {GameSession.SelectedCharacter} | modo {GameSession.Mode} | host: {IsServer}");

        if (itemSpawner == null) itemSpawner = FindAnyObjectByType<ItemSpawner>();

        itemSeed.OnValueChanged += OnItemSeedChanged;

        if (IsServer)
        {
            itemSeed.Value = Random.Range(1, int.MaxValue);
            SpawnItems(itemSeed.Value);
            StartCoroutine(AssignOwnershipWhenReady());
        }
        else if (itemSeed.Value != 0)
        {
            // O cliente já recebeu a semente ao entrar
            SpawnItems(itemSeed.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        itemSeed.OnValueChanged -= OnItemSeedChanged;
        base.OnNetworkDespawn();
    }

    private void OnItemSeedChanged(int oldValue, int newValue)
    {
        // O host já sorteou ao gerar a semente; o cliente sorteia quando ela chega
        if (IsServer || newValue == 0) return;
        SpawnItems(newValue);
    }

    private void SpawnItems(int seed)
    {
        if (itemSpawner == null)
        {
            Debug.LogWarning("GameNetworkSetup: não há ItemSpawner na cena.", this);
            return;
        }

        Debug.Log($"[Rede] Semente dos itens: {seed}");
        itemSpawner.Spawn(seed);
    }

    private IEnumerator AssignOwnershipWhenReady()
    {
        float remaining = timeout;

        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;

            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
            bool playersReady = players.Length >= 2;

            foreach (PlayerController p in players)
            {
                NetworkObject no = p.GetComponent<NetworkObject>();
                if (no == null || !no.IsSpawned) playersReady = false;
            }

            bool clientConnected = NetworkManager.Singleton.ConnectedClientsIds.Count >= 2;

            if (playersReady && clientConnected)
            {
                Assign(players);
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("GameNetworkSetup: os jogadores ou o cliente não ficaram prontos a tempo. Confirma que o Axel e o Mike têm NetworkObject.", this);
    }

    private void Assign(PlayerController[] players)
    {
        CharacterType hostCharacter = GameSession.SelectedCharacter ?? CharacterType.Axel;

        ulong clientId = NetworkManager.ServerClientId;
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id != NetworkManager.ServerClientId)
            {
                clientId = id;
                break;
            }
        }

        foreach (PlayerController p in players)
        {
            NetworkObject no = p.GetComponent<NetworkObject>();
            ulong target = p.Character == hostCharacter ? NetworkManager.ServerClientId : clientId;

            if (no.OwnerClientId != target)
                no.ChangeOwnership(target);

            Debug.Log($"[Rede] {p.Character} -> dono {target}");
        }
    }
}
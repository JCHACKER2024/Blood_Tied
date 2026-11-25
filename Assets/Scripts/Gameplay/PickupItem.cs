using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickupItem : Interactable
{
    [Header("Info do item")]
    [SerializeField] private string itemName = "Pista";
    [TextArea]
    [SerializeField] private string description;
    [Tooltip("Imagem principal (inventário e vista grande).")]
    [SerializeField] private Sprite icon;
    [Tooltip("Frames extra ao examinar. Vazio = o E só amplia.")]
    [SerializeField] private Sprite[] examineFrames;

    [Header("Pontos (dados quando o item é recolhido)")]
    [Tooltip("Pontos que este item dá ao Axel (negativo se o incriminar).")]
    [SerializeField] private int axelPoints = 0;
    [Tooltip("Pontos que este item dá ao Mike (negativo se o incriminar).")]
    [SerializeField] private int mikePoints = 0;

    [Header("Quem pode apanhar / fotografar (tecla E)")]
    [SerializeField] private bool axelCanPickup = true;
    [SerializeField] private bool mikeCanPickup = true;

    [Header("Remover do mapa (Apenas para o Axel)")]
    [Tooltip("Axel: guarda o objeto na mochila e remove-o do mapa.")]
    [SerializeField] private bool axelRemovesItem = true;

    [Header("Esconder (tecla F) - Objetos Incriminatórios")]
    [SerializeField] private bool canBeHiddenByAxel = false;
    [SerializeField] private bool canBeHiddenByMike = false;
    [Tooltip("Tempo em segundos para esconder o item.")]
    [SerializeField] private float hideDuration = 5f;

    [Header("Detetar testemunha (mesma sala)")]
    [Tooltip("Distância em unidades na Unity para considerar que o parceiro está na mesma sala.")]
    [SerializeField] private float witnessRadius = 9f;

    [Header("UI de Progresso")]
    [SerializeField] private GameObject progressContainer;
    [SerializeField] private Slider progressSlider;

    private Coroutine hideCoroutine;
    private bool isBeingHidden;
    private readonly HashSet<CharacterType> inspectedBy = new HashSet<CharacterType>();
    private CharacterType? pendingHideCharacter;

    public string ItemName => itemName;

    // Mudado de Start para Awake para evitar o aviso CS0114 com a classe pai Interactable
    private void Awake()
    {
        if (progressContainer != null)
            progressContainer.SetActive(false);
    }

    private void OnEnable()
    {
        if (ItemViewer.Instance != null)
            ItemViewer.Instance.InspectionClosed += HandleInspectionClosed;
    }

    private void OnDisable()
    {
        if (ItemViewer.Instance != null)
            ItemViewer.Instance.InspectionClosed -= HandleInspectionClosed;

        pendingHideCharacter = null;
    }

    private void HandleInspectionClosed(ItemInfo closedItem)
    {
        if (!pendingHideCharacter.HasValue) return;
        if (closedItem == null || closedItem.name != itemName) return;

        CharacterType character = pendingHideCharacter.Value;
        pendingHideCharacter = null;

        // Atualiza o prompt caso o jogador continue perto do item
        PlayerController player = FindPlayer(character);
        if (player != null)
        {
            UpdatePromptText(player);
        }
    }

    private PlayerController FindPlayer(CharacterType character)
    {
        PlayerController[] foundPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
        foreach (PlayerController p in foundPlayers)
        {
            if (p != null && p.Character == character)
                return p;
        }
        return null;
    }

    private ItemInfo CreateItemInfo()
    {
        return new ItemInfo(itemName, description, icon, examineFrames);
    }

    public override string GetInteractPrompt(PlayerController player)
    {
        if (isBeingHidden || !CanInteract(player)) return string.Empty;

        return player.Character == CharacterType.Mike
            ? "Press E to take picture!"
            : "Press E to grab the item!";
    }

    public override string GetSecondaryInteractPrompt(PlayerController player)
    {
        if (isBeingHidden || !CanSecondaryInteract(player)) return string.Empty;

        return inspectedBy.Contains(player.Character)
            ? "Press F to hide the item!"
            : "Press F to inspect!";
    }

    public override bool CanInteract(PlayerController player)
    {
        if (isBeingHidden) return false;

        bool allowed = player.Character == CharacterType.Axel ? axelCanPickup : mikeCanPickup;
        if (!allowed) return false;

        return Inventory.Instance == null || !Inventory.Instance.Has(player.Character, itemName);
    }

    public override void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;

        if (Inventory.Instance == null)
        {
            Debug.LogWarning("PickupItem: não há Inventory na cena.", this);
            return;
        }

        Inventory.Instance.Add(player.Character, CreateItemInfo());

        // Os pontos entram na conta dos dois (uma única vez por item)
        GameSession.RegisterCollected(itemName, axelPoints, mikePoints);
        Debug.Log($"[Pontos] {itemName} recolhido por {player.Character} -> Axel: {GameSession.GetScore(CharacterType.Axel)} | Mike: {GameSession.GetScore(CharacterType.Mike)}");

        bool removed = player.Character == CharacterType.Axel && axelRemovesItem;

        // Online: avisa o outro PC
        ItemNetworkSync.NotifyCollected(itemName, axelPoints, mikePoints, removed);

        if (removed)
        {
            Destroy(gameObject);
        }
    }

    public override bool CanSecondaryInteract(PlayerController player)
    {
        if (isBeingHidden || GameSession.IsHidden(itemName) || GameSession.HasBeenProcessed(itemName))
            return false;

        return player.Character == CharacterType.Axel ? canBeHiddenByAxel : canBeHiddenByMike;
    }

    public override void SecondaryInteract(PlayerController player)
    {
        if (!CanSecondaryInteract(player)) return;

        // 1.ª vez com F: abre o ItemViewer para inspecionar antes de permitir esconder
        if (!inspectedBy.Contains(player.Character))
        {
            inspectedBy.Add(player.Character);

            if (ItemViewer.Instance != null)
            {
                pendingHideCharacter = player.Character;
                ItemViewer.Instance.Show(CreateItemInfo(), notifyWhenClosed: true);
            }
            else
            {
                Debug.LogWarning("PickupItem: ItemViewer.Instance não encontrado, a esconder diretamente.", this);
                StartHideProcess(player);
            }
            return;
        }

        StartHideProcess(player);
    }

    private void StartHideProcess(PlayerController player)
    {
        if (IsPartnerNearby(player.Character))
        {
            CaughtInTheAct(player.Character);
            return;
        }

        hideCoroutine = StartCoroutine(HideRoutine(player));
    }

    private IEnumerator HideRoutine(PlayerController player)
    {
        isBeingHidden = true;
        Vector3 startPos = player.transform.position;
        float elapsed = 0f;

        // O slider só aparece no ecrã do jogador que está ativamente a executar a ação
        bool showUI = player.IsControlled;

        if (showUI && progressContainer != null)
        {
            progressContainer.SetActive(true);
            if (progressSlider != null) progressSlider.value = 1f;
        }

        while (elapsed < hideDuration)
        {
            if (IsPartnerNearby(player.Character))
            {
                CancelHide(caught: true, hider: player.Character);
                yield break;
            }

            if (Vector2.Distance(startPos, player.transform.position) > 0.25f)
            {
                CancelHide(caught: false, hider: player.Character);
                yield break;
            }

            elapsed += Time.deltaTime;

            // A barra decresce de 1 para 0 suavemente
            if (showUI && progressSlider != null)
                progressSlider.value = 1f - Mathf.Clamp01(elapsed / hideDuration);

            yield return null;
        }

        FinishHide(player.Character);
    }

    private bool IsPartnerNearby(CharacterType hider)
    {
        PlayerController[] foundPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
        foreach (PlayerController p in foundPlayers)
        {
            if (p != null && p.Character != hider)
            {
                if (Vector2.Distance(transform.position, p.transform.position) <= witnessRadius)
                    return true;
            }
        }
        return false;
    }

    private void CancelHide(bool caught, CharacterType hider)
    {
        isBeingHidden = false;
        if (progressContainer != null) progressContainer.SetActive(false);

        if (caught)
        {
            CaughtInTheAct(hider);
        }
        else
        {
            Debug.Log($"[Co-op] {hider} interrompeu a ação de esconder {itemName}.");
        }
    }

    private void CaughtInTheAct(CharacterType hider)
    {
        GameSession.HideItem(itemName, hider);
        GameSession.ExposeItem(itemName);
        gameObject.SetActive(true);

        // Online: avisa o outro PC
        ItemNetworkSync.NotifyHidden(itemName, hider, true);

        Debug.Log($"[Co-op] {hider} foi apanhado em flagrante pelo parceiro a tentar esconder {itemName}!");
    }

    private void FinishHide(CharacterType hider)
    {
        isBeingHidden = false;
        if (progressContainer != null) progressContainer.SetActive(false);

        GameSession.HideItem(itemName, hider);

        // Online: avisa o outro PC (antes de desativar o objeto)
        ItemNetworkSync.NotifyHidden(itemName, hider, false);

        gameObject.SetActive(false);
        Debug.Log($"[Co-op] {hider} escondeu {itemName} com sucesso!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, witnessRadius);
    }
}
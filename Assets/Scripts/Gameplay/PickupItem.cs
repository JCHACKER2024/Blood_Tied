using System.Collections;
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

    // Mudado de Start para Awake para evitar o aviso CS0114 com a classe pai Interactable
    private void Awake()
    {
        if (progressContainer != null)
            progressContainer.SetActive(false);
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

        return "Press F to hide the item!";
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

        Inventory.Instance.Add(player.Character, new ItemInfo(itemName, description, icon, examineFrames));

        if (player.Character == CharacterType.Axel && axelRemovesItem)
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
        Debug.Log($"[Co-op] {hider} foi apanhado em flagrante pelo parceiro a tentar esconder {itemName}!");
    }

    private void FinishHide(CharacterType hider)
    {
        isBeingHidden = false;
        if (progressContainer != null) progressContainer.SetActive(false);

        GameSession.HideItem(itemName, hider);
        gameObject.SetActive(false);
        Debug.Log($"[Co-op] {hider} escondeu {itemName} com sucesso!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, witnessRadius);
    }
}
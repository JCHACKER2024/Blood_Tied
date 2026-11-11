using UnityEngine;

public class PickupItem : Interactable
{
    [Header("Info do item")]
    [SerializeField] private string itemName = "Pista";
    [TextArea]
    [SerializeField] private string description;
    [Tooltip("Imagem principal (inventário e vista grande).")]
    [SerializeField] private Sprite icon;
    [Tooltip("Frames extra ao examinar (ex: o verso da foto). Vazio = o E só amplia.")]
    [SerializeField] private Sprite[] examineFrames;

    [Header("Quem pode apanhar (tecla E)")]
    [SerializeField] private bool axelCanPickup = true;
    [SerializeField] private bool mikeCanPickup = true;

    [Header("Desaparece depois de apanhado?")]
    [Tooltip("Axel: guarda o objeto na mochila (sai do jogo).")]
    [SerializeField] private bool axelRemovesItem = true;
    [Tooltip("Mike: tira uma foto (o objeto fica no sítio).")]
    [SerializeField] private bool mikeRemovesItem = false;

    [Header("Esconder (tecla F)")]
    [Tooltip("Só o Mike tem esta mecânica. Marca nos itens que ele pode esconder.")]
    [SerializeField] private bool canBeHiddenByMike = false;

    // Retorna a mensagem certa para o E dependendo do personagem ativo
    public override string GetInteractPrompt(PlayerController player)
    {
        if (!CanInteract(player)) return string.Empty;

        if (player.Character == CharacterType.Mike)
        {
            return "Press E to take picture!";
        }

        return "Press E to grab the item!";
    }

    // Retorna a mensagem do F (apenas para o Mike se puder esconder)
    public override string GetSecondaryInteractPrompt(PlayerController player)
    {
        if (!CanSecondaryInteract(player)) return string.Empty;

        return "Press F to hide the item!";
    }

    public override bool CanInteract(PlayerController player)
    {
        bool allowed = player.Character == CharacterType.Axel ? axelCanPickup : mikeCanPickup;
        if (!allowed) return false;

        // Já tem este item no inventário: não repete
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

        bool remove = player.Character == CharacterType.Axel ? axelRemovesItem : mikeRemovesItem;
        if (remove) Destroy(gameObject);
    }

    public override bool CanSecondaryInteract(PlayerController player)
    {
        return player.Character == CharacterType.Mike
               && canBeHiddenByMike
               && !GameSession.IsHidden(itemName);
    }

    public override void SecondaryInteract(PlayerController player)
    {
        if (!CanSecondaryInteract(player)) return;

        GameSession.HideItem(itemName);
        gameObject.SetActive(false);
    }
}
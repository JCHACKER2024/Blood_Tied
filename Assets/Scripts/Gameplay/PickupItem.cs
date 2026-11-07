using UnityEngine;

public class PickupItem : Interactable
{
    [Header("Info do item")]
    [SerializeField] private string itemName = "Pista";
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Quem pode apanhar")]
    [SerializeField] private bool axelCanPickup = true;
    [SerializeField] private bool mikeCanPickup = true;

    [Header("Desaparece depois de apanhado?")]
    [Tooltip("Axel: guarda o objeto na mochila (sai do jogo).")]
    [SerializeField] private bool axelRemovesItem = true;
    [Tooltip("Mike: tira uma foto (o objeto fica no sítio).")]
    [SerializeField] private bool mikeRemovesItem = false;

    public override bool CanInteract(PlayerController player)
    {
        bool allowed = player.Character == CharacterType.Axel ? axelCanPickup : mikeCanPickup;
        if (!allowed) return false;

        // Já tem este item: não repete
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

        Inventory.Instance.Add(player.Character, new ItemInfo(itemName, description, icon));

        bool remove = player.Character == CharacterType.Axel ? axelRemovesItem : mikeRemovesItem;
        if (remove) Destroy(gameObject);
    }
}
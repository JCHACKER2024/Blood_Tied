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

    public override bool CanInteract(PlayerController player)
    {
        return player.Character == CharacterType.Axel ? axelCanPickup : mikeCanPickup;
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
        Destroy(gameObject);
    }
}
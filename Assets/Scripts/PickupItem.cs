using UnityEngine;

public class PickupItem : Interactable
{
    [SerializeField] private string itemName = "Pista";
    [TextArea]
    [SerializeField] private string description;

    public string ItemName => itemName;
    public string Description => description;

    public override void Interact(PlayerController player)
    {
        if (Inventory.Instance != null)
            Inventory.Instance.Add(itemName);

        Destroy(gameObject);
    }
}
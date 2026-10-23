using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.45f, 0.07f, 0.07f, 1f);

    private InventoryUI owner;

    public ItemInfo Item { get; private set; }

    public void Setup(InventoryUI ui, ItemInfo info)
    {
        owner = ui;
        Item = info;

        bool hasIcon = info != null && info.icon != null;
        icon.enabled = hasIcon;
        icon.sprite = hasIcon ? info.icon : null;

        SetSelected(false);
    }

    public void SetSelected(bool value)
    {
        if (background != null)
            background.color = value ? selectedColor : normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item != null) owner.Hover(Item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Item != null) owner.Hover(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Item != null) owner.Select(Item);
    }
}
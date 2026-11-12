using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotoSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image photo;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(0.45f, 0.53f, 0.58f, 1f);
    [SerializeField] private Color selectedColor = Color.white;

    private CameraInventoryUI owner;

    public ItemInfo Item { get; private set; }

    public void Setup(CameraInventoryUI ui, ItemInfo info)
    {
        owner = ui;
        Item = info;

        bool hasPhoto = info != null && info.icon != null;
        photo.enabled = hasPhoto;
        photo.sprite = hasPhoto ? info.icon : null;

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
        if (Item != null) owner.OnSlotClicked(Item);
    }
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AnswerSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    private Button button;

    public void Setup(ItemInfo item, Action<ItemInfo> onClick)
    {
        button = GetComponent<Button>();

        if (label != null) label.text = item.name;

        if (icon != null)
        {
            bool hasIcon = item.icon != null;
            icon.enabled = hasIcon;
            icon.sprite = hasIcon ? item.icon : null;
        }

        button.onClick.AddListener(() => onClick(item));
    }

    public void SetInteractable(bool value)
    {
        if (button != null) button.interactable = value;
    }
}
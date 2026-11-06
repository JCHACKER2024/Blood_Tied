using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("Painel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;

    [Header("Grelha")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private InventorySlot slotPrefab;
    [SerializeField] private int minSlots = 9;
    [Tooltip("Tem de ser igual ao Constraint Count do Grid Layout Group.")]
    [SerializeField] private int columns = 3;

    [Header("Detalhe")]
    [SerializeField] private Image detailImage;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private string emptyDetailText = "Seleciona um item.";

    private readonly List<InventorySlot> slots = new List<InventorySlot>();
    private PlayerController[] players;
    private CharacterType shown;
    private ItemInfo selected;

    private void Awake()
    {
        IsOpen = false;
    }

    private void Start()
    {
        players = FindObjectsByType<PlayerController>();
        panel.SetActive(false);
        ShowDetail(null);

        if (Inventory.Instance != null)
            Inventory.Instance.ItemAdded += OnItemAdded;
    }

    private void OnDestroy()
    {
        IsOpen = false;

        if (Inventory.Instance != null)
            Inventory.Instance.ItemAdded -= OnItemAdded;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame)
        {
            if (IsOpen)
            {
                Close();
            }
            else if (!ScreenFader.Busy && TryGetControlled(out CharacterType c))
            {
                Open(c, null);
            }
        }
        else if (IsOpen && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
        {
            Close();
        }
    }

    private bool TryGetControlled(out CharacterType character)
    {
        foreach (PlayerController p in players)
        {
            if (p != null && p.IsControlled)
            {
                character = p.Character;
                return true;
            }
        }

        character = CharacterType.Axel;
        return false;
    }

    private void OnItemAdded(CharacterType character, ItemInfo item)
    {
        Open(character, item);
    }

    public void Open(CharacterType character, ItemInfo highlight)
    {
        shown = character;
        selected = highlight;
        IsOpen = true;
        panel.SetActive(true);

        if (titleText != null) titleText.text = $"Inventário - {character}";

        Refresh();
        ShowDetail(highlight);
    }

    public void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
    }

    public void Hover(ItemInfo item)
    {
        ShowDetail(item != null ? item : selected);
    }

    public void Select(ItemInfo item)
    {
        selected = item;
        UpdateSelection();
        ShowDetail(item);
    }

    private void Refresh()
    {
        IReadOnlyList<ItemInfo> items = Inventory.Instance.GetItems(shown);

        int total = Mathf.Max(minSlots, items.Count);
        total = Mathf.CeilToInt(total / (float)columns) * columns;

        while (slots.Count < total)
            slots.Add(Instantiate(slotPrefab, gridParent));

        for (int i = 0; i < slots.Count; i++)
        {
            bool active = i < total;
            slots[i].gameObject.SetActive(active);
            if (!active) continue;

            slots[i].Setup(this, i < items.Count ? items[i] : null);
        }

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        foreach (InventorySlot s in slots)
            s.SetSelected(s.Item != null && s.Item == selected);
    }

    private void ShowDetail(ItemInfo item)
    {
        bool has = item != null;

        if (detailImage != null)
        {
            detailImage.enabled = has && item.icon != null;
            detailImage.sprite = has ? item.icon : null;
        }

        if (detailName != null) detailName.text = has ? item.name : "";
        if (detailDescription != null) detailDescription.text = has ? item.description : emptyDetailText;
    }
}
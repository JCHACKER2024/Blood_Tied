using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private static bool inventoryOpen;

    // true com o inventário OU a vista grande de um item abertos (o player fica parado)
    public static bool IsOpen => inventoryOpen || ItemViewer.IsOpen;

    [Header("Painel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;

    [Header("Grelha")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private InventorySlot slotPrefab;
    [SerializeField] private int minSlots = 15;
    [Tooltip("Tem de ser igual ao Constraint Count do Grid Layout Group.")]
    [SerializeField] private int columns = 5;

    [Header("Detalhe")]
    [SerializeField] private Image detailImage;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private string emptyDetailText = "Seleciona um item. (E - Examinar)";

    private readonly List<InventorySlot> slots = new List<InventorySlot>();
    private PlayerController[] players;
    private CharacterType shown;
    private ItemInfo selected;

    private void Awake()
    {
        inventoryOpen = false;
    }

    private void Start()
    {
        players = FindObjectsByType<PlayerController>();
        panel.SetActive(false);
        ShowDetail(null);
    }

    private void OnDestroy()
    {
        inventoryOpen = false;
    }

    private void Update()
    {
        // A vista grande tem prioridade sobre as teclas do inventário
        if (ItemViewer.IsOpen || ItemViewer.ChangedThisFrame) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame)
        {
            if (inventoryOpen)
            {
                Close();
            }
            else if (!ScreenFader.Busy && TryGetControlled(out CharacterType c))
            {
                Open(c, null);
            }
        }
        else if (inventoryOpen && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
        {
            Close();
        }
        else if (inventoryOpen && kb.eKey.wasPressedThisFrame && selected != null && ItemViewer.Instance != null)
        {
            ItemViewer.Instance.Show(selected);
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

    public void Open(CharacterType character, ItemInfo highlight)
    {
        shown = character;
        selected = highlight;
        inventoryOpen = true;
        panel.SetActive(true);

        if (titleText != null) titleText.text = $"Inventário - {character}";

        Refresh();
        ShowDetail(highlight);
    }

    public void Close()
    {
        inventoryOpen = false;
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
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraInventoryUI : MonoBehaviour
{
    private static bool cameraOpen;
    private static int lastChangeFrame = -1;

    public static bool IsOpen => cameraOpen;
    public static bool ChangedThisFrame => lastChangeFrame == Time.frameCount;

    [Header("Painel")]
    [SerializeField] private GameObject panel;

    [Header("Ecrã da câmara (grelha de fotos)")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private PhotoSlot slotPrefab;
    [SerializeField] private int minSlots = 15;
    [Tooltip("Tem de ser igual ao Constraint Count do Grid Layout Group.")]
    [SerializeField] private int columns = 5;

    [Header("Foto em grande")]
    [SerializeField] private GameObject bigPhotoRoot;
    [SerializeField] private Image bigPhotoImage;
    [SerializeField] private Button bigPhotoButton;

    [Header("Textos")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text countText;
    [Tooltip("{0} = número de fotos. Ex: 'FOTOS {0}'")]
    [SerializeField] private string countFormat = "{0}";
    [SerializeField] private string emptyDescription = "Ainda sem fotos.";

    [Header("Botões da câmara (opcional)")]
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button centerButton;
    [SerializeField] private Button powerButton;

    private readonly List<PhotoSlot> slots = new List<PhotoSlot>();
    private int selectedIndex = -1;
    private bool bigOpen;

    private void Awake()
    {
        cameraOpen = false;
        lastChangeFrame = -1;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        CloseBig();

        Bind(upButton, () => Move(0, -1));
        Bind(downButton, () => Move(0, 1));
        Bind(leftButton, () => Move(-1, 0));
        Bind(rightButton, () => Move(1, 0));
        Bind(centerButton, ToggleBig);
        Bind(powerButton, Close);
        Bind(bigPhotoButton, CloseBig);

        if (Inventory.Instance != null)
            Inventory.Instance.ItemAdded += OnItemAdded;
    }

    private void OnDestroy()
    {
        cameraOpen = false;

        if (Inventory.Instance != null)
            Inventory.Instance.ItemAdded -= OnItemAdded;
    }

    private void Update()
    {
        if (!cameraOpen || ChangedThisFrame) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Se a foto em grande estiver aberta: E ou Esc fecham-na
        if (bigOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
            {
                CloseBig();
            }
            return; // Bloqueia a navegação da grelha enquanto a foto está em grande
        }

        // Navegação normal na grelha da câmara
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            Move(-1, 0);
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            Move(1, 0);
        else if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            Move(0, -1);
        else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            Move(0, 1);
        else if (kb.eKey.wasPressedThisFrame)
            OpenBig(); // Tecla E abre a foto selecionada em grande
        else if (kb.escapeKey.wasPressedThisFrame)
            Close();
        else if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            Close();
    }

    private void OnItemAdded(CharacterType character, ItemInfo item)
    {
        if (character == CharacterType.Mike)
            Open(item);
    }

    public void Open(ItemInfo highlight)
    {
        cameraOpen = true;
        lastChangeFrame = Time.frameCount;

        if (panel != null) panel.SetActive(true);
        CloseBig();
        Refresh();

        IReadOnlyList<ItemInfo> items = GameSession.GetItems(CharacterType.Mike);

        if (items.Count == 0)
            selectedIndex = -1;
        else if (highlight != null)
            selectedIndex = Mathf.Max(0, IndexOf(highlight));
        else
            selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Count - 1);

        UpdateSelection();
        UpdateDescription(null);
    }

    public void Close()
    {
        cameraOpen = false;
        lastChangeFrame = Time.frameCount;

        CloseBig();
        if (panel != null) panel.SetActive(false);
    }

    public void Move(int dx, int dy)
    {
        IReadOnlyList<ItemInfo> items = GameSession.GetItems(CharacterType.Mike);
        if (items.Count == 0) return;

        int next = selectedIndex < 0 ? 0 : selectedIndex + dx + dy * columns;
        SetSelected(Mathf.Clamp(next, 0, items.Count - 1));
    }

    public void Hover(ItemInfo item)
    {
        UpdateDescription(item);
    }

    // Chamado quando se clica diretamente numa foto com o rato
    public void OnSlotClicked(ItemInfo item)
    {
        int index = IndexOf(item);
        if (index < 0) return;

        SetSelected(index);
        OpenBig(); // Clicar abre logo em grande diretamente
    }

    public void CloseBig()
    {
        bigOpen = false;
        if (bigPhotoRoot != null) bigPhotoRoot.SetActive(false);
    }

    private void ToggleBig()
    {
        if (bigOpen) CloseBig();
        else OpenBig();
    }

    private void OpenBig()
    {
        if (SelectedItem() == null) return;

        bigOpen = true;
        if (bigPhotoRoot != null) bigPhotoRoot.SetActive(true);
        ShowBig();
    }

    private void ShowBig()
    {
        ItemInfo item = SelectedItem();
        if (item == null)
        {
            CloseBig();
            return;
        }

        if (bigPhotoImage != null)
        {
            bigPhotoImage.sprite = item.icon;
            bigPhotoImage.enabled = item.icon != null;
        }
    }

    private void SetSelected(int index)
    {
        selectedIndex = index;
        UpdateSelection();
        UpdateDescription(null);

        if (bigOpen) ShowBig();
    }

    private void Refresh()
    {
        IReadOnlyList<ItemInfo> items = GameSession.GetItems(CharacterType.Mike);

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

        if (countText != null)
            countText.text = string.Format(countFormat, items.Count);
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].SetSelected(i == selectedIndex && slots[i].Item != null);
    }

    private void UpdateDescription(ItemInfo preview)
    {
        if (descriptionText == null) return;

        ItemInfo shown = preview != null ? preview : SelectedItem();
        descriptionText.text = shown != null ? shown.description : emptyDescription;
    }

    private ItemInfo SelectedItem()
    {
        IReadOnlyList<ItemInfo> items = GameSession.GetItems(CharacterType.Mike);
        return (selectedIndex >= 0 && selectedIndex < items.Count) ? items[selectedIndex] : null;
    }

    private int IndexOf(ItemInfo item)
    {
        IReadOnlyList<ItemInfo> items = GameSession.GetItems(CharacterType.Mike);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item) return i;
        }
        return -1;
    }

    private static void Bind(Button button, UnityAction action)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            action();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        });
    }
}
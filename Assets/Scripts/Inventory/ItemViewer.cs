using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemViewer : MonoBehaviour
{
    public static ItemViewer Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    // Evita que a mesma tecla seja usada por 2 scripts no mesmo frame
    public static bool ChangedThisFrame => lastChangeFrame == Time.frameCount;
    private static int lastChangeFrame = -1;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text hintText;

    [Header("Examinar")]
    [Tooltip("Ampliação usada nos itens sem frames extra.")]
    [SerializeField] private float zoomScale = 1.3f;
    [SerializeField] private string hint = "E - Examinar     Esc - Fechar";

    private ItemInfo current;
    private int frameIndex;
    private bool zoomed;

    private void Awake()
    {
        Instance = this;
        IsOpen = false;
        lastChangeFrame = -1;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

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
        if (!IsOpen || ChangedThisFrame) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.eKey.wasPressedThisFrame)
            Examine();
        else if (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Close();
    }

    private void OnItemAdded(CharacterType character, ItemInfo item)
    {
        Show(item);
    }

    public void Show(ItemInfo item)
    {
        if (item == null) return;

        current = item;
        frameIndex = 0;
        zoomed = false;

        IsOpen = true;
        lastChangeFrame = Time.frameCount;

        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        lastChangeFrame = Time.frameCount;

        current = null;
        panel.SetActive(false);
    }

    private void Examine()
    {
        if (current == null) return;

        // Com frames extra: passa ao seguinte. Sem frames extra: amplia / reduz.
        if (current.FrameCount > 1)
            frameIndex = (frameIndex + 1) % current.FrameCount;
        else
            zoomed = !zoomed;

        Refresh();
    }

    private void Refresh()
    {
        Sprite sprite = current.GetFrame(frameIndex);

        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null;
        itemImage.rectTransform.localScale = zoomed ? Vector3.one * zoomScale : Vector3.one;

        if (nameText != null) nameText.text = current.name;
        if (descriptionText != null) descriptionText.text = current.description;
        if (hintText != null) hintText.text = hint;
    }
}
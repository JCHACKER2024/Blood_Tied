using UnityEngine;
using TMPro;

public abstract class Interactable : MonoBehaviour
{
    [Tooltip("O GameObject filho que tem o componente TextMeshPro (World Space).")]
    [SerializeField] private GameObject prompt;

    private TextMeshPro tmpText;

    protected virtual void Start()
    {
        if (prompt != null)
        {
            // Como é TextMeshPro 3D/World Space, usa o componente 'TextMeshPro' (e não TextMeshProUGUI)
            tmpText = prompt.GetComponentInChildren<TextMeshPro>(true);
        }

        SetHighlighted(false, null);
    }

    public virtual void SetHighlighted(bool value, PlayerController player = null)
    {
        if (prompt == null) return;

        if (prompt == gameObject)
        {
            Debug.LogWarning($"{name}: o Prompt não pode ser o próprio objeto.", this);
            return;
        }

        if (value && player != null)
        {
            UpdatePromptText(player);
        }

        prompt.SetActive(value);
    }

    public virtual void UpdatePromptText(PlayerController player)
    {
        if (tmpText == null && prompt != null)
        {
            tmpText = prompt.GetComponentInChildren<TextMeshPro>(true);
        }

        if (tmpText == null) return;

        bool canE = CanInteract(player);
        bool canF = CanSecondaryInteract(player);

        string textE = canE ? GetInteractPrompt(player) : string.Empty;
        string textF = canF ? GetSecondaryInteractPrompt(player) : string.Empty;

        // Se puder fazer as duas ações (Mike num item que se pode esconder), junta com \n
        if (!string.IsNullOrEmpty(textE) && !string.IsNullOrEmpty(textF))
        {
            tmpText.text = textE + "\n" + textF;
        }
        else if (!string.IsNullOrEmpty(textE))
        {
            tmpText.text = textE;
        }
        else if (!string.IsNullOrEmpty(textF))
        {
            tmpText.text = textF;
        }
        else
        {
            tmpText.text = string.Empty;
        }
    }

    public virtual string GetInteractPrompt(PlayerController player)
    {
        return "Press E to grab the item!";
    }

    public virtual string GetSecondaryInteractPrompt(PlayerController player)
    {
        return "Press F to hide the item!";
    }

    public virtual bool CanInteract(PlayerController player)
    {
        return true;
    }

    public abstract void Interact(PlayerController player);

    public virtual bool CanSecondaryInteract(PlayerController player)
    {
        return false;
    }

    public virtual void SecondaryInteract(PlayerController player)
    {
    }
}
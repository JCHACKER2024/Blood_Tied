using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Tooltip("Opcional: um FILHO (ex: texto 'E') que aparece quando o player está perto. Não pôr a própria porta.")]
    [SerializeField] private GameObject prompt;

    protected virtual void Start()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(bool value)
    {
        if (prompt == null) return;

        if (prompt == gameObject)
        {
            Debug.LogWarning($"{name}: o Prompt não pode ser o próprio objeto. Deixa vazio ou usa um filho.", this);
            return;
        }

        prompt.SetActive(value);
    }

    // Se devolver false, este player ignora o objeto (não destaca nem interage)
    public virtual bool CanInteract(PlayerController player)
    {
        return true;
    }

    public abstract void Interact(PlayerController player);
}
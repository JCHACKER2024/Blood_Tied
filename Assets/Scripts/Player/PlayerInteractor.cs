using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    private PlayerController player;
    private readonly List<Interactable> nearby = new List<Interactable>();
    private Interactable current;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Interactable i = other.GetComponentInParent<Interactable>();
        if (i != null && !nearby.Contains(i)) nearby.Add(i);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Interactable i = other.GetComponentInParent<Interactable>();
        if (i != null) nearby.Remove(i);
    }

    private void Update()
    {
        if (!player.IsControlled || ScreenFader.Busy)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(GetClosest());

        Keyboard kb = Keyboard.current;
        if (current != null && kb != null && kb.eKey.wasPressedThisFrame)
            current.Interact(player);
    }

    private Interactable GetClosest()
    {
        nearby.RemoveAll(i => i == null);

        Interactable best = null;
        float bestDist = float.MaxValue;

        foreach (Interactable i in nearby)
        {
            if (!i.CanInteract(player)) continue;

            float d = Mathf.Abs(i.transform.position.x - transform.position.x);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }

    private void SetCurrent(Interactable next)
    {
        if (next == current) return;

        if (current != null) current.SetHighlighted(false);
        current = next;
        if (current != null) current.SetHighlighted(true);
    }
}
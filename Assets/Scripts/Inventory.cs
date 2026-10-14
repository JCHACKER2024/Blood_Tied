using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Tooltip("Opcional: texto no ecrã com o contador de pistas.")]
    [SerializeField] private TMP_Text counterText;

    private readonly List<string> collected = new List<string>();
    private int totalItems;

    public IReadOnlyList<string> Collected => collected;
    public int TotalItems => totalItems;
    public bool AllCollected => totalItems > 0 && collected.Count >= totalItems;

    public event Action<string> ItemCollected;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        totalItems = FindObjectsByType<PickupItem>().Length;
        UpdateUI();
    }

    public void Add(string itemName)
    {
        collected.Add(itemName);
        Debug.Log($"Apanhou: {itemName} ({collected.Count}/{totalItems})");
        ItemCollected?.Invoke(itemName);
        UpdateUI();
    }

    public bool Has(string itemName)
    {
        return collected.Contains(itemName);
    }

    private void UpdateUI()
    {
        if (counterText != null)
            counterText.text = $"Pistas: {collected.Count}/{totalItems}";
    }
}
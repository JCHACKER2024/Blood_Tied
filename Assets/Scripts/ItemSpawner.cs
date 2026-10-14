using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public PickupItem item;
        public Transform[] spawnPoints;
    }

    [SerializeField] private SpawnEntry[] entries;
    [Tooltip("Se ligado, dois itens nunca ficam no mesmo ponto.")]
    [SerializeField] private bool uniquePoints = true;

    private const int MaxAttempts = 50;

    private void Awake()
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (TryAssign(out List<(PickupItem item, Transform point)> result))
            {
                foreach (var (item, point) in result)
                {
                    Vector3 pos = point.position;
                    pos.z = item.transform.position.z;
                    item.transform.position = pos;
                    Debug.Log($"ItemSpawner: {item.name} -> {point.name} ({point.position.x:0.0}, {point.position.y:0.0})");
                }
                return;
            }
        }

        Debug.LogWarning("ItemSpawner: não foi possível atribuir pontos a todos os itens. Põe mais pontos de spawn.", this);
    }

    private bool TryAssign(out List<(PickupItem, Transform)> result)
    {
        result = new List<(PickupItem, Transform)>();
        HashSet<Transform> used = new HashSet<Transform>();

        // Ordem aleatória dos itens, para nenhum ter prioridade fixa
        List<SpawnEntry> order = new List<SpawnEntry>(entries);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (SpawnEntry entry in order)
        {
            if (entry.item == null || entry.spawnPoints == null || entry.spawnPoints.Length == 0)
                continue;

            List<Transform> options = new List<Transform>();
            foreach (Transform p in entry.spawnPoints)
            {
                if (p != null && (!uniquePoints || !used.Contains(p)))
                    options.Add(p);
            }

            if (options.Count == 0) return false;

            Transform chosen = options[Random.Range(0, options.Count)];
            used.Add(chosen);
            result.Add((entry.item, chosen));
        }

        return true;
    }
}
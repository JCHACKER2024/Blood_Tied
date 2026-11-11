using System;
using UnityEngine;
using UnityEngine.UI;

public class TabGroup : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public Button button;
        public GameObject panel;
        [Tooltip("Opcional: sublinhado ou marcador que só aparece no separador ativo.")]
        public GameObject selectedMarker;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private int startIndex = 0;

    private void Awake()
    {
        if (tabs == null) return;

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => Select(index));
        }
    }

    // Sempre que o catálogo abre, volta ao primeiro separador
    private void OnEnable()
    {
        Select(startIndex);
    }

    public void Select(int index)
    {
        if (tabs == null) return;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = i == index;
            if (tabs[i].panel != null) tabs[i].panel.SetActive(active);
            if (tabs[i].selectedMarker != null) tabs[i].selectedMarker.SetActive(active);
        }
    }
}
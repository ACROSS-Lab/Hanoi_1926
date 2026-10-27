using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Serializable]
    public class TabItem
    {
        public string tabName;
        public Button tabButton;
        public GameObject tabContentPanel;
        public Image tabButtonBackground;
        public TextMeshProUGUI tabButtonText;
    }

    [Header("Tabs Configuration")]
    [SerializeField] private List<TabItem> tabs = new();
    [SerializeField] private int defaultTabIndex = 0;

    [Header("Styling")]
    [SerializeField] private Color activeTabColor = new Color(0.05f, 0.23f, 0.40f, 1f); // Dark blue
    [SerializeField] private Color inactiveTabColor = new Color(0.85f, 0.85f, 0.85f, 1f); // Light gray
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    public event Action<int> OnTabChanged;

    private int _currentTabIndex = -1;

    private void Start()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i;
            if (tabs[i].tabButton != null)
            {
                tabs[i].tabButton.onClick.AddListener(() => SelectTab(index));
            }
        }

        SelectTab(Mathf.Clamp(defaultTabIndex, 0, Mathf.Max(0, tabs.Count - 1)));
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;
        _currentTabIndex = index;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == index);

            // Toggle Panel
            if (tabs[i].tabContentPanel != null)
            {
                tabs[i].tabContentPanel.SetActive(isActive);
            }

            // Visual Styling
            if (tabs[i].tabButtonBackground != null)
            {
                tabs[i].tabButtonBackground.color = isActive ? activeTabColor : inactiveTabColor;
            }

            if (tabs[i].tabButtonText != null)
            {
                tabs[i].tabButtonText.color = isActive ? activeTextColor : inactiveTextColor;
            }
        }

        OnTabChanged?.Invoke(index);
    }

    public int GetCurrentTabIndex() => _currentTabIndex;
}

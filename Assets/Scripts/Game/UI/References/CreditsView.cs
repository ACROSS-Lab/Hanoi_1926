using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreditsView : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Leave null to load from Resources/References/CreditsData")]
    [SerializeField] private TextAsset csvAsset;
    [SerializeField] private string resourcesPath = "References/CreditsData";

    [Header("UI Hierarchy")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private GameObject creditRowPrefab;

    [Header("Settings")]
    [SerializeField] private bool reloadOnLanguageChanged = true;

    private readonly List<CreditEntry> _entries = new();
    private readonly List<(TextMeshProUGUI roleText, string roleKey)> _localizedRows = new();

    private void Start()
    {
        LoadData();
        BuildCredits();

        if (reloadOnLanguageChanged)
        {
            LocalizationManager.OnLanguageChanged += RefreshLocalization;
        }
    }

    private void OnDestroy()
    {
        if (reloadOnLanguageChanged)
        {
            LocalizationManager.OnLanguageChanged -= RefreshLocalization;
        }
    }

    public void LoadData()
    {
        _entries.Clear();

        TextAsset targetAsset = csvAsset;
        if (targetAsset == null)
        {
            targetAsset = Resources.Load<TextAsset>(resourcesPath);
        }

        if (targetAsset == null)
        {
            Debug.LogError($"[CreditsView] CSV not found at 'Resources/{resourcesPath}'.");
            return;
        }

        List<List<string>> rows = ReferenceCSVParser.Parse(targetAsset.text);
        if (rows.Count <= 1) return; // Skip header

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count >= 1)
            {
                string roleKey = row[0].Trim();
                string nameValue = row.Count >= 2 ? row[1].Trim() : string.Empty;

                _entries.Add(new CreditEntry
                {
                    Role = roleKey,
                    Name = nameValue
                });
            }
        }
    }

    public void BuildCredits()
    {
        if (rowsContainer == null || creditRowPrefab == null)
        {
            Debug.LogWarning("[CreditsView] Missing rowsContainer or creditRowPrefab reference.");
            return;
        }

        foreach (Transform child in rowsContainer)
        {
            Destroy(child.gameObject);
        }
        _localizedRows.Clear();

        foreach (var entry in _entries)
        {
            GameObject rowObj = Instantiate(creditRowPrefab, rowsContainer);

            string localizedRole = GetLocalizedText(entry.Role);

            // Expected components: [0] = Role, [1] = Name
            TextMeshProUGUI[] textComponents = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length >= 2)
            {
                textComponents[0].text = localizedRole;
                textComponents[1].text = entry.Name;

                _localizedRows.Add((textComponents[0], entry.Role));
            }
        }
    }

    public void RefreshLocalization()
    {
        foreach (var (roleText, key) in _localizedRows)
        {
            if (roleText != null)
            {
                roleText.text = GetLocalizedText(key);
            }
        }
    }

    private string GetLocalizedText(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetLocalizedValue(key);
        }
        return key;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreditsView : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Leave null to load from Resources/References/CreditsData")]
    [SerializeField] private TextAsset csvAsset;
    [SerializeField] private string resourcesPath = "References/CreditsData";

    [Header("UI Hierarchy")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private GameObject creditRowPrefab;
    [Tooltip("Optional: dedicated prefab for Department headers. If null, creditRowPrefab is adapted automatically.")]
    [SerializeField] private GameObject headerPrefab;

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
                if (string.IsNullOrWhiteSpace(roleKey)) continue;

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
            bool isHeader = string.IsNullOrWhiteSpace(entry.Name);
            GameObject prefabToUse = (isHeader && headerPrefab != null) ? headerPrefab : creditRowPrefab;
            GameObject rowObj = Instantiate(prefabToUse, rowsContainer);

            string localizedRole = GetLocalizedText(entry.Role);
            TextMeshProUGUI[] textComponents = rowObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (isHeader)
            {
                if (headerPrefab != null)
                {
                    if (textComponents.Length > 0)
                    {
                        textComponents[0].text = localizedRole;
                        _localizedRows.Add((textComponents[0], entry.Role));
                    }
                }
                else
                {
                    // Fallback with creditRowPrefab: adapt to display as full-width header
                    if (textComponents.Length >= 2)
                    {
                        textComponents[1].transform.parent.gameObject.SetActive(false);
                    }

                    if (textComponents.Length >= 1)
                    {
                        textComponents[0].text = localizedRole;
                        textComponents[0].alignment = TextAlignmentOptions.Center;
                        textComponents[0].fontStyle = FontStyles.Bold;

                        var layoutElement = textComponents[0].GetComponentInParent<LayoutElement>();
                        if (layoutElement != null)
                        {
                            layoutElement.preferredWidth = -1;
                            layoutElement.flexibleWidth = 1;
                        }

                        _localizedRows.Add((textComponents[0], entry.Role));
                    }
                }
            }
            else
            {
                // Expected components: [0] = Role, [1] = Name
                if (textComponents.Length >= 2)
                {
                    textComponents[0].text = localizedRole;

                    string formattedNames = entry.Name.Replace("\\n", "\n");
                    formattedNames = TMP_LinkHandler.FormatLinks(formattedNames);
                    textComponents[1].text = formattedNames;

                    if (textComponents[1].GetComponent<TMP_LinkHandler>() == null)
                    {
                        textComponents[1].gameObject.AddComponent<TMP_LinkHandler>();
                    }

                    _localizedRows.Add((textComponents[0], entry.Role));
                }
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

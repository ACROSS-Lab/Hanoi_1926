using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class POIReferenceTable : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Leave null to load from Resources/References/POIReferences")]
    [SerializeField] private TextAsset csvAsset;
    [SerializeField] private string resourcesPath = "References/POIReferences";

    [Header("UI Hierarchy")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private GameObject rowPrefab;

    [Header("Settings")]
    [SerializeField] private bool reloadOnLanguageChanged = true;

    private readonly List<POIReferenceEntry> _entries = new();
    private readonly List<(TextMeshProUGUI poiNameText, string key)> _localizedRows = new();

    private void Start()
    {
        LoadData();
        BuildTable();

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
            Debug.LogError($"[POIReferenceTable] CSV not found at 'Resources/{resourcesPath}'.");
            return;
        }

        List<List<string>> rows = ReferenceCSVParser.Parse(targetAsset.text);
        if (rows.Count <= 1) return; // Skip header

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count >= 3)
            {
                _entries.Add(new POIReferenceEntry
                {
                    LocalizationKey = row[0].Trim(),
                    NameReference = row[1].Trim(),
                    ImageReference = row[2].Trim()
                });
            }
        }
    }

    public void BuildTable()
    {
        if (rowsContainer == null || rowPrefab == null)
        {
            Debug.LogWarning("[POIReferenceTable] Missing rowsContainer or rowPrefab reference.");
            return;
        }

        foreach (Transform child in rowsContainer)
        {
            Destroy(child.gameObject);
        }
        _localizedRows.Clear();

        foreach (var entry in _entries)
        {
            GameObject rowObj = Instantiate(rowPrefab, rowsContainer);

            string localizedPoiName = GetLocalizedText(entry.LocalizationKey);
            string formattedNameRef = TMP_LinkHandler.FormatLinks(entry.NameReference);
            string formattedImageRef = TMP_LinkHandler.FormatLinks(entry.ImageReference);

            // Expected components: [0] = POI Name, [1] = Name Source, [2] = Image Source
            TextMeshProUGUI[] textComponents = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length >= 3)
            {
                textComponents[0].text = localizedPoiName;
                textComponents[1].text = formattedNameRef;
                textComponents[2].text = formattedImageRef;

                // Ensure link handlers are attached
                if (textComponents[1].GetComponent<TMP_LinkHandler>() == null)
                {
                    textComponents[1].gameObject.AddComponent<TMP_LinkHandler>();
                }
                if (textComponents[2].GetComponent<TMP_LinkHandler>() == null)
                {
                    textComponents[2].gameObject.AddComponent<TMP_LinkHandler>();
                }

                _localizedRows.Add((textComponents[0], entry.LocalizationKey));
            }
        }
    }

    public void RefreshLocalization()
    {
        foreach (var (poiNameText, key) in _localizedRows)
        {
            if (poiNameText != null)
            {
                poiNameText.text = GetLocalizedText(key);
            }
        }
    }

    private string GetLocalizedText(string key)
    {
        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetLocalizedValue(key);
        }
        return key;
    }
}

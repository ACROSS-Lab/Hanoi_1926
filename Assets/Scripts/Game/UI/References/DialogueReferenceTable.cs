using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueReferenceTable : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Leave null to load from Resources/References/DialogueReferences")]
    [SerializeField] private TextAsset csvAsset;
    [SerializeField] private string resourcesPath = "References/DialogueReferences";

    [Header("UI Hierarchy")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private GameObject rowPrefab;

    [Header("Settings")]
    [SerializeField] private bool reloadOnLanguageChanged = true;

    private readonly List<DialogueReferenceEntry> _entries = new();
    private readonly List<(TextMeshProUGUI dialogueText, string dialogueKey, TextMeshProUGUI referenceText, string referenceKey)> _localizedRows = new();

    private void Start()
    {
        LoadData();
        BuildTable();

        if (reloadOnLanguageChanged)
        {
            LocalizationManager.OnLanguageChanged += RefreshLocalization;
        }
    }

    private void OnEnable()
    {
        if (_localizedRows.Count > 0)
        {
            RebuildTableLayout();
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
            Debug.LogError($"[DialogueReferenceTable] CSV not found at 'Resources/{resourcesPath}'.");
            return;
        }

        List<List<string>> rows = ReferenceCSVParser.Parse(targetAsset.text);
        if (rows.Count <= 1) return; // Skip header or empty

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count >= 2)
            {
                _entries.Add(new DialogueReferenceEntry
                {
                    LocalizationKey = row[0].Trim(),
                    Reference = row[1].Trim()
                });
            }
        }
    }

    public void BuildTable()
    {
        if (rowsContainer == null || rowPrefab == null)
        {
            Debug.LogWarning("[DialogueReferenceTable] Missing rowsContainer or rowPrefab reference.");
            return;
        }

        // Clear existing children
        foreach (Transform child in rowsContainer)
        {
            Destroy(child.gameObject);
        }
        _localizedRows.Clear();

        foreach (var entry in _entries)
        {
            GameObject rowObj = Instantiate(rowPrefab, rowsContainer);

            // Fetch localized dialogue and reference strings
            string localizedDialogue = GetLocalizedText(entry.LocalizationKey);
            string formattedReference = GetFormattedReference(entry.Reference);

            // Find TextMeshPro components in rowPrefab:
            // Expected: index 0 = Dialogue, index 1 = Reference
            TextMeshProUGUI[] textComponents = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length >= 2)
            {
                textComponents[0].text = localizedDialogue;
                textComponents[1].text = formattedReference;

                // Ensure right cell has TMP_LinkHandler attached
                if (textComponents[1].GetComponent<TMP_LinkHandler>() == null)
                {
                    textComponents[1].gameObject.AddComponent<TMP_LinkHandler>();
                }

                _localizedRows.Add((textComponents[0], entry.LocalizationKey, textComponents[1], entry.Reference));
            }
        }
    }

    public void RefreshLocalization()
    {
        foreach (var (dialogueText, dialogueKey, referenceText, referenceKey) in _localizedRows)
        {
            if (dialogueText != null)
            {
                dialogueText.text = GetLocalizedText(dialogueKey);
            }

            if (referenceText != null)
            {
                referenceText.text = GetFormattedReference(referenceKey);
            }
        }

        RebuildTableLayout();
    }

    private void RebuildTableLayout()
    {
        if (rowsContainer == null) return;

        var refresher = rowsContainer.GetComponent<UIOrientationRefresher>();
        if (refresher != null)
        {
            refresher.TriggerRefresh();
        }
        else if (rowsContainer is RectTransform rectTransform)
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

    private string GetFormattedReference(string referenceKey)
    {
        string localizedReference = GetLocalizedText(referenceKey);
        localizedReference = localizedReference.Replace("\\n", "\n");
        return TMP_LinkHandler.FormatLinks(localizedReference);
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

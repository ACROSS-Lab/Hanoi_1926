using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Leave null to load from Resources/References/POIReferences")]
    [SerializeField] private TextAsset csvAsset;
    [SerializeField] private string resourcesPath = "References/POIReferences";
    [SerializeField] private string imagesFolderPath = "References/Images";

    [Header("UI Hierarchy")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private GameObject rowPrefab;

    [Header("Settings")]
    [SerializeField] private bool reloadOnLanguageChanged = true;

    private readonly List<POIReferenceEntry> _entries = new();
    private readonly List<(TextMeshProUGUI captionText, string captionKey, TextMeshProUGUI sourceText, string sourceKey)> _localizedRows = new();

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
            Debug.LogError($"[ImagePanel] CSV not found at 'Resources/{resourcesPath}'.");
            return;
        }

        List<List<string>> rows = ReferenceCSVParser.Parse(targetAsset.text);
        if (rows.Count <= 1) return; // Skip header

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count >= 3)
            {
                string imgKey = row[0].Trim();
                string capKey = string.IsNullOrWhiteSpace(row[1]) ? imgKey : row[1].Trim();
                string refKey = row[2].Trim();
                _entries.Add(new POIReferenceEntry
                {
                    ImageKey = imgKey,
                    LocalizationKey = capKey,
                    Reference = refKey
                });
            }
            else if (row.Count >= 2)
            {
                string key = row[0].Trim();
                string refKey = row[1].Trim();
                _entries.Add(new POIReferenceEntry
                {
                    ImageKey = key,
                    LocalizationKey = key,
                    Reference = refKey
                });
            }
        }
    }

    public void BuildTable()
    {
        if (rowsContainer == null || rowPrefab == null)
        {
            Debug.LogWarning("[ImagePanel] Missing rowsContainer or rowPrefab reference.");
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

            // 1. Load and assign Image (Column 1)
            Image photoImage = FindPhotoImage(rowObj);
            if (photoImage != null)
            {
                Sprite sprite = LoadSprite(entry.ImageKey);
                if (sprite != null)
                {
                    photoImage.sprite = sprite;
                    photoImage.enabled = true;
                }
                else
                {
                    Debug.LogWarning($"[ImagePanel] Sprite not found at 'Resources/{imagesFolderPath}/{entry.ImageKey}'.");
                }
            }

            // 2. Fetch localized Caption (Column 2) and Source (Column 3)
            string localizedCaption = GetFormattedText(entry.LocalizationKey);
            string formattedSource = GetFormattedReference(entry.Reference);

            // Expected TextMeshProUGUI components in ImageRowPrefab:
            // [0] = Caption (Chú thích), [1] = Source (Nguồn)
            TextMeshProUGUI[] textComponents = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length >= 2)
            {
                textComponents[0].text = localizedCaption;
                textComponents[1].text = formattedSource;

                if (textComponents[1].GetComponent<TMP_LinkHandler>() == null)
                {
                    textComponents[1].gameObject.AddComponent<TMP_LinkHandler>();
                }

                _localizedRows.Add((textComponents[0], entry.LocalizationKey, textComponents[1], entry.Reference));
            }
        }

        RebuildTableLayout();
    }

    public void RefreshLocalization()
    {
        foreach (var (captionText, captionKey, sourceText, sourceKey) in _localizedRows)
        {
            if (captionText != null)
            {
                captionText.text = GetFormattedText(captionKey);
            }

            if (sourceText != null)
            {
                sourceText.text = GetFormattedReference(sourceKey);
            }
        }

        RebuildTableLayout();
    }

    private Image FindPhotoImage(GameObject rowObj)
    {
        Transform directChild = rowObj.transform.Find("Left/Container/Image");
        if (directChild != null && directChild.TryGetComponent<Image>(out var img))
        {
            return img;
        }

        var fitter = rowObj.GetComponentInChildren<AspectRatioFitter>(true);
        if (fitter != null)
        {
            var fitterImg = fitter.GetComponentInChildren<Image>(true);
            if (fitterImg != null) return fitterImg;
        }

        return null;
    }

    private Sprite LoadSprite(string imageKey)
    {
        if (string.IsNullOrWhiteSpace(imageKey)) return null;

        string fullPath = string.IsNullOrWhiteSpace(imagesFolderPath)
            ? imageKey
            : $"{imagesFolderPath.TrimEnd('/')}/{imageKey}";

        Sprite sprite = Resources.Load<Sprite>(fullPath);
        if (sprite != null) return sprite;

        Texture2D tex = Resources.Load<Texture2D>(fullPath);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        return null;
    }

    private void RebuildTableLayout()
    {
        if (rowsContainer == null) return;

        var refresher = rowsContainer.GetComponent<UIOrientationRefresher>();
        if (refresher == null)
        {
            refresher = rowsContainer.gameObject.AddComponent<UIOrientationRefresher>();
        }

        refresher.TriggerRefresh();
    }

    private string GetFormattedText(string key)
    {
        string text = GetLocalizedText(key);
        return text.Replace("\\n", "\n");
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

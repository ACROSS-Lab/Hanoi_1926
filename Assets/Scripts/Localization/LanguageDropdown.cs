using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Connects a Dropdown (TMP_Dropdown or UI.Dropdown) to the LocalizationManager.
/// Reads the selected option's title (text) and forwards it to LocalizationManager.Instance.SetLanguage().
/// </summary>
public class LanguageDropdown : MonoBehaviour
{
    [Header("Dropdown References (Auto-detected if left empty)")]
    [Tooltip("TextMeshPro Dropdown component. Will be auto-assigned if on the same GameObject.")]
    [SerializeField] private TMP_Dropdown tmpDropdown;

    [Tooltip("Legacy Unity UI Dropdown component. Will be auto-assigned if on the same GameObject.")]
    [SerializeField] private Dropdown unityDropdown;

    [Header("Settings")]
    [Tooltip("Keep the dropdown selection synchronized with the active language at start and when changed elsewhere.")]
    [SerializeField] private bool syncWithActiveLanguage = true;

    [Tooltip("If true, automatically hooks into the dropdown's onValueChanged event at runtime without needing manual Inspector setup.")]
    [SerializeField] private bool autoSubscribeAtRuntime = false;

    private void Awake()
    {
        // Auto-detect components if not manually assigned in inspector
        if (tmpDropdown == null)
        {
            tmpDropdown = GetComponent<TMP_Dropdown>();
        }

        if (unityDropdown == null)
        {
            unityDropdown = GetComponent<Dropdown>();
        }
    }

    private void OnEnable()
    {
        if (syncWithActiveLanguage)
        {
            LocalizationManager.OnLanguageChanged += SyncDropdownWithCurrentLanguage;
        }

        if (autoSubscribeAtRuntime)
        {
            if (tmpDropdown != null)
            {
                tmpDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
            if (unityDropdown != null)
            {
                unityDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }

        SyncDropdownWithCurrentLanguage();
    }

    private void Start()
    {
        // Secondary sync in Start to ensure LocalizationManager.Awake has executed
        SyncDropdownWithCurrentLanguage();
    }

    private void OnDisable()
    {
        if (syncWithActiveLanguage)
        {
            LocalizationManager.OnLanguageChanged -= SyncDropdownWithCurrentLanguage;
        }

        if (autoSubscribeAtRuntime)
        {
            if (tmpDropdown != null)
            {
                tmpDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
            if (unityDropdown != null)
            {
                unityDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }
    }

    private void Reset()
    {
        // Automatically find dropdown component when script is attached in Editor
        tmpDropdown = GetComponent<TMP_Dropdown>();
        unityDropdown = GetComponent<Dropdown>();
    }

    /// <summary>
    /// Subscribe this method to the Dropdown's "On Value Changed (Int32)" in the Inspector.
    /// IMPORTANT: In the inspector method picker, select this under the "Dynamic int" section at the top.
    /// </summary>
    /// <param name="index">The selected item index passed automatically by Unity.</param>
    public void OnDropdownValueChanged(int index)
    {
        ChangeLanguageByIndex(index);
    }

    /// <summary>
    /// Looks up the title of the item at the given index and sets the language.
    /// </summary>
    public void ChangeLanguageByIndex(int index)
    {
        string languageTitle = GetOptionTitle(index);

        if (string.IsNullOrEmpty(languageTitle))
        {
            Debug.LogWarning($"[LanguageDropdown] No valid title found for dropdown index {index} on {gameObject.name}.");
            return;
        }

        ChangeLanguage(languageTitle);
    }

    /// <summary>
    /// Forwards the language title string to LocalizationManager.
    /// </summary>
    /// <param name="languageTitle">The title matching the CSV column header (e.g., 'Vietnamese', 'English', 'French').</param>
    public void ChangeLanguage(string languageTitle)
    {
        if (string.IsNullOrEmpty(languageTitle)) return;

        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[LanguageDropdown] LocalizationManager.Instance is null! Ensure LocalizationManager is loaded in the scene.");
            return;
        }

        // Avoid re-triggering if already set to this language
        if (string.Equals(LocalizationManager.Instance.GetLanguage(), languageTitle, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        LocalizationManager.Instance.SetLanguage(languageTitle);
    }

    /// <summary>
    /// Retrieves the text title of a dropdown option by index.
    /// </summary>
    private string GetOptionTitle(int index)
    {
        if (tmpDropdown != null)
        {
            if (index >= 0 && index < tmpDropdown.options.Count)
            {
                return CleanTitle(tmpDropdown.options[index].text);
            }
        }
        else if (unityDropdown != null)
        {
            if (index >= 0 && index < unityDropdown.options.Count)
            {
                return CleanTitle(unityDropdown.options[index].text);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Strips any rich text formatting tags (e.g., color, bold) and trims leading/trailing whitespace.
    /// </summary>
    private string CleanTitle(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        // Strip HTML/TMP rich text tags like <color=...>, <b>, etc.
        string cleaned = Regex.Replace(rawText, "<.*?>", string.Empty);
        return cleaned.Trim();
    }

    /// <summary>
    /// Synchronizes the dropdown selection to match the active language in LocalizationManager without firing change events.
    /// </summary>
    public void SyncDropdownWithCurrentLanguage()
    {
        if (LocalizationManager.Instance == null) return;

        string currentLanguage = LocalizationManager.Instance.GetLanguage();
        if (string.IsNullOrEmpty(currentLanguage)) return;

        if (tmpDropdown != null)
        {
            for (int i = 0; i < tmpDropdown.options.Count; i++)
            {
                if (string.Equals(CleanTitle(tmpDropdown.options[i].text), currentLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    if (tmpDropdown.value != i)
                    {
                        tmpDropdown.SetValueWithoutNotify(i);
                    }
                    return;
                }
            }
        }
        else if (unityDropdown != null)
        {
            for (int i = 0; i < unityDropdown.options.Count; i++)
            {
                if (string.Equals(CleanTitle(unityDropdown.options[i].text), currentLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    if (unityDropdown.value != i)
                    {
                        unityDropdown.SetValueWithoutNotify(i);
                    }
                    return;
                }
            }
        }
    }
}

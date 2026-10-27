using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMP_LinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI _textMeshPro;
    private Canvas _parentCanvas;
    private Camera _eventCamera;

    // Static URL registry to prevent URLs exceeding TextMeshPro's 128-character tag buffer limit (m_htmlTag)
    private static readonly Dictionary<string, string> LinkIdToUrl = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> UrlToLinkId = new Dictionary<string, string>();
    private static int _nextLinkId = 0;

    private void Awake()
    {
        _textMeshPro = GetComponent<TextMeshProUGUI>();
        _parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_textMeshPro == null) return;

        // Resolve camera based on canvas render mode
        if (_parentCanvas != null && _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _eventCamera = null;
        }
        else
        {
            _eventCamera = eventData.pressEventCamera ?? Camera.main;
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textMeshPro, eventData.position, _eventCamera);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = _textMeshPro.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();

            if (!string.IsNullOrEmpty(linkId))
            {
                // Check if the linkId was registered in the lookup table
                if (LinkIdToUrl.TryGetValue(linkId, out string realUrl))
                {
                    Debug.Log($"Opening registered URL [{linkId}]: {realUrl}");
                    Application.OpenURL(realUrl);
                }
                else
                {
                    // Fallback if linkId is directly a full URL
                    Debug.Log($"Opening direct URL: {linkId}");
                    Application.OpenURL(linkId);
                }
            }
        }
    }

    /// <summary>
    /// Utility to format all plain http/https URLs inside a string into clickable TMP links.
    /// Uses short IDs (e.g. <link="L0">) so long URLs never exceed TextMeshPro's 128-char tag limit!
    /// </summary>
    public static string FormatLinks(string input, string linkColorHex = "#1e88e5")
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Regex matching http or https URLs
        string pattern = @"(https?://[^\s\)\""'>]+)";
        return Regex.Replace(input, pattern, match =>
        {
            string url = match.Value.TrimEnd('.', ',', ';', ':', '\"', '\'');
            string linkId = RegisterUrl(url);

            return $"<color={linkColorHex}><link=\"{linkId}\"><u>{url}</u></link></color>";
        });
    }

    private static string RegisterUrl(string url)
    {
        if (UrlToLinkId.TryGetValue(url, out string existingId))
        {
            return existingId;
        }

        string newId = $"L{_nextLinkId++}";
        LinkIdToUrl[newId] = url;
        UrlToLinkId[url] = newId;
        return newId;
    }
}

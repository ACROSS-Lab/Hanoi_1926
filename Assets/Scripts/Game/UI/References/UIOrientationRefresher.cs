using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIOrientationRefresher : MonoBehaviour
{
    private Vector2 _lastResolution;
    private Coroutine _refreshRoutine;

    private void Start()
    {
        _lastResolution = new Vector2(Screen.width, Screen.height);
    }

    private void Update()
    {
        // Detect phone orientation flip or window resize
        if (Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
        {
            _lastResolution = new Vector2(Screen.width, Screen.height);
            TriggerRefresh();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        TriggerRefresh();
    }

    public void TriggerRefresh()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_refreshRoutine != null) StopCoroutine(_refreshRoutine);
        _refreshRoutine = StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        // Wait 1 frame for CanvasScaler to apply the new screen aspect ratio
        yield return null;

        // Force all row ContentSizeFitters to recalculate height for the new column widths
        ContentSizeFitter[] fitters = GetComponentsInChildren<ContentSizeFitter>(true);
        foreach (var fitter in fitters)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(fitter.GetComponent<RectTransform>());
        }

        // Rebuild parent container and ScrollRect
        var parentLayout = GetComponent<VerticalLayoutGroup>();
        if (parentLayout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
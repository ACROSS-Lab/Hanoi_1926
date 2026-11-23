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
        TriggerRefresh();
    }

    private void OnEnable()
    {
        TriggerRefresh();
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
        // Wait 1 frame for CanvasScaler and ScrollRect Viewport to apply the new screen aspect ratio
        yield return null;

        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        // Sync LayoutElement.preferredHeight on image cells with their current width (1:1 aspect ratio)
        AspectRatioFitter[] aspectFitters = GetComponentsInChildren<AspectRatioFitter>(true);
        foreach (var aspectFitter in aspectFitters)
        {
            if (aspectFitter == null || aspectFitter.transform.parent == null) continue;

            RectTransform cellRect = aspectFitter.transform.parent as RectTransform;
            LayoutElement cellLayout = aspectFitter.transform.parent.GetComponent<LayoutElement>();
            if (cellRect != null && cellLayout != null)
            {
                float currentWidth = cellRect.rect.width;
                if (currentWidth > 0f && aspectFitter.aspectRatio > 0f)
                {
                    float targetHeight = currentWidth / aspectFitter.aspectRatio;
                    if (!Mathf.Approximately(cellLayout.preferredHeight, targetHeight))
                    {
                        cellLayout.preferredHeight = targetHeight;
                    }
                }
            }
        }

        // Force all row ContentSizeFitters to recalculate height for the new column widths & image heights
        ContentSizeFitter[] fitters = GetComponentsInChildren<ContentSizeFitter>(true);
        foreach (var fitter in fitters)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(fitter.GetComponent<RectTransform>());
        }

        // Rebuild parent container and ScrollRect
        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }
    }
}

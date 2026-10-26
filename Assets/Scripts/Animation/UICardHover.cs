using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Balatro-like hover effect for UI elements:
/// slight scale up and a tilt toward the mouse position.
/// Add this to any UI element with a raycast target (Image, etc.).
/// Uses the new Input System.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UICardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;

    [Header("Tilt")]
    [SerializeField] private float maxTilt = 15f;
    [SerializeField] private float tiltDuration = 0.15f;

    private RectTransform rect;
    private Canvas canvas;
    private bool isHovered;
    private Tween rotationTween;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (!isHovered || Mouse.current == null) return;

        // Mouse position in the local space of this element
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mousePosition, cam, out Vector2 localPoint);

        // Normalize to -1..1 (center of the element = 0)
        float x = Mathf.Clamp(localPoint.x / (rect.rect.width * 0.5f), -1f, 1f);
        float y = Mathf.Clamp(localPoint.y / (rect.rect.height * 0.5f), -1f, 1f);

        // Tilt toward the mouse
        Vector3 targetRotation = new Vector3(-y * maxTilt, x * maxTilt, 0f);

        rotationTween?.Kill();
        rotationTween = rect.DOLocalRotate(targetRotation, tiltDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        rect.DOKill(); // kill scale tween
        rect.DOScale(hoverScale, scaleDuration).SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        rotationTween?.Kill();
        rect.DOKill();
        rect.DOScale(1f, scaleDuration).SetEase(Ease.OutQuad);
        rect.DOLocalRotate(Vector3.zero, tiltDuration).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        rect.DOKill();
    }
}

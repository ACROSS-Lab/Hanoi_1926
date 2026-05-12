using UnityEngine;
using DG.Tweening;

public class Oscillation : MonoBehaviour
{
    public enum TweenMode { PositionY, Scale }

    [SerializeField] private TweenMode tweenMode = TweenMode.PositionY;

    [Header("Position Y Settings")]
    [SerializeField] private float amplitude = 1f;

    [Header("Scale Settings")]
    [SerializeField] private float scaleMultiplier = 1.5f;

    [Header("Common Settings")]
    [SerializeField] private float duration = 1f;

    private void Start()
    {
        switch (tweenMode)
        {
            case TweenMode.PositionY:
                float startY = transform.position.y;
                transform.DOMoveY(startY + amplitude, duration)
                         .SetEase(Ease.InOutQuart)
                         .SetLoops(-1, LoopType.Yoyo);
                break;

            case TweenMode.Scale:
                Vector3 startScale = transform.localScale;
                Vector3 targetScale = startScale * scaleMultiplier;
                transform.DOScale(targetScale, duration)
                         .SetEase(Ease.InOutQuart)
                         .SetLoops(-1, LoopType.Yoyo);
                break;
        }
    }
}
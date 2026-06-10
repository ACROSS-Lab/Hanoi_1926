using UnityEngine;
using DG.Tweening;

public class CanvasGroupFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float targetAlpha = 1f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutQuad;

    public void FadeIn()
    {
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f; 
        gameObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        canvasGroup.DOFade(1f, duration)
            .SetEase(easeType);
    }

    public void FadeOut()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasGroup.DOFade(0f, duration)
            .SetEase(easeType)
            .OnComplete(() => gameObject.SetActive(false));
    }

    public void PlayFade()
    {
        canvasGroup.DOFade(targetAlpha, duration).SetEase(easeType);
    }
}
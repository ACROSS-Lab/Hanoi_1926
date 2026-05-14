using UnityEngine;
using DG.Tweening;

public class SpritePopIn : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private Ease easeType = Ease.OutElastic;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popSound;
    [SerializeField][Range(0f, 1f)] private float volume = 1f;
    [SerializeField][Range(0.8f, 1.2f)] private float pitch = 1f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField][Range(0f, 0.2f)] private float pitchVariance = 0.1f;

    private Vector3 _originalScale;
    private Tween _currentTween;

    private void Awake()
    {
        _originalScale = transform.localScale;

        // Auto-create an AudioSource if none is assigned
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        _currentTween?.Kill();

        PlayPopSound();

        _currentTween = transform
            .DOScale(_originalScale, duration)
            .SetEase(easeType)
            .SetUpdate(false);
    }

    private void OnDisable()
    {
        _currentTween?.Kill();
        transform.localScale = _originalScale;
    }

    private void PlayPopSound()
    {
        if (audioSource == null || popSound == null) return;

        audioSource.pitch = randomizePitch
            ? pitch + Random.Range(-pitchVariance, pitchVariance)
            : pitch;

        // PlayOneShot allows sound overlap if the object is toggled rapidly
        audioSource.PlayOneShot(popSound, volume);
    }
}
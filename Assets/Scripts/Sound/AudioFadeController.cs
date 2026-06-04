using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioFadeController : MonoBehaviour
{
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 2.0f;

    [Range(0f, 1f)]
    public float targetVolume = 1f;

    private AudioSource _audioSource;
    private Coroutine _currentFade;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _audioSource.volume = 0f;
        _audioSource.Play();

        if (_currentFade != null) StopCoroutine(_currentFade);
        _currentFade = StartCoroutine(Fade(0f, targetVolume, fadeInDuration));
    }

    public void FadeOutAndDisable()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_currentFade != null) StopCoroutine(_currentFade);
        _currentFade = StartCoroutine(Fade(_audioSource.volume, 0f, fadeOutDuration, disableAfter: true));
    }
    private IEnumerator Fade(float from, float to, float duration, bool disableAfter = false)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        _audioSource.volume = to;

        if (disableAfter)
        {
            _audioSource.Stop();
            gameObject.SetActive(false);
        }
    }
}
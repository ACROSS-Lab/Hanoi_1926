using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] float fadeInDuration = 1.0f;
    [SerializeField] float fadeOutDuration = 2.0f;
    [SerializeField] bool fadeInOnStart = true;

    public float FadeInDuration => fadeInDuration;
    public float FadeOutDuration => fadeOutDuration;

    CanvasGroup fadeCanvasGroup;
    bool isTransitioning = false;

    void Awake()
    {
        isTransitioning = false;
        fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        if (fadeInOnStart && fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }
    }

    void Start()
    {
        if (fadeInOnStart)
        {
            StartCoroutine(Fade(0f, fadeOutDuration));
        }
    }

    public void SwitchScene(string sceneName)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void SwitchScene(int sceneBuildIndex)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(TransitionRoutineIndex(sceneBuildIndex));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        fadeCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(Fade(1f, fadeInDuration));

        // Unload additive diorama if open
        if (PlayerCameraController.ActiveDioramaHandle.IsValid())
        {
            yield return UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(PlayerCameraController.ActiveDioramaHandle);
            PlayerCameraController.ActiveDioramaHandle = default;
        }

        // Unload main addressable scene if loaded via Addressables
        if (AddressableSceneLoader.LastLoadedSceneHandle.IsValid())
        {
            yield return UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(AddressableSceneLoader.LastLoadedSceneHandle);
            AddressableSceneLoader.LastLoadedSceneHandle = default;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(Fade(0f, fadeOutDuration));

        fadeCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    IEnumerator TransitionRoutineIndex(int sceneBuildIndex)
    {
        fadeCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(Fade(1f, fadeInDuration));

        // Unload additive diorama if open
        if (PlayerCameraController.ActiveDioramaHandle.IsValid())
        {
            yield return UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(PlayerCameraController.ActiveDioramaHandle);
            PlayerCameraController.ActiveDioramaHandle = default;
        }

        // Unload main addressable scene if loaded via Addressables
        if (AddressableSceneLoader.LastLoadedSceneHandle.IsValid())
        {
            yield return UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(AddressableSceneLoader.LastLoadedSceneHandle);
            AddressableSceneLoader.LastLoadedSceneHandle = default;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneBuildIndex);
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(Fade(0f, fadeOutDuration));

        fadeCanvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    public IEnumerator Fade(float targetAlpha, float fadeDuration)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}

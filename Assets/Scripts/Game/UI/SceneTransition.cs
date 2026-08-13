using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    public Canvas canvas;
    public CanvasGroup fadeCanvasGroup;
    public float fadeInDuration = 1.0f;
    public float fadeOutDuration = 2.0f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += SetCamera;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= SetCamera;
    }

    void SetCamera(Scene scene, LoadSceneMode mode)
    {
        canvas.worldCamera = Camera.allCameras[Camera.allCameras.Length - 1];
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SwitchScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        fadeCanvasGroup.blocksRaycasts = true;

        yield return StartCoroutine(Fade(1f, fadeInDuration));

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(Fade(0f, fadeOutDuration));

        fadeCanvasGroup.blocksRaycasts = false;
    }

    IEnumerator Fade(float targetAlpha, float fadeDuration)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }
}
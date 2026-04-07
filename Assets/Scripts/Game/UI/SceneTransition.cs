using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public Canvas canvas;
    public CanvasGroup fadeCanvasGroup;
    public GameObject leftHand, rightHand;
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
        DontDestroyOnLoad(gameObject);
    }

    public void SwitchScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        yield return StartCoroutine(Fade(1f, fadeInDuration));
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        rightHand.SetActive(false);
        leftHand.SetActive(false);
        
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(Fade(0f, fadeOutDuration));

        Destroy(gameObject);
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
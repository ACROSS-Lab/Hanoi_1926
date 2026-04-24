using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PlayerTransition : MonoBehaviour
{
    public static PlayerTransition Instance { get; private set; }

    [Header("Fading Setup")]
    [SerializeField] CanvasGroup fadeCanvasGroup;
    [SerializeField] float fadeDuration = 0.5f;

    [Header("Object to keep")]
    [SerializeField] GameObject objectToKeep;

    [Header("Hide Hands Ray")]
    [SerializeField] GameObject[] hiddenObjects;

    List<GameObject> primarySceneRoots = new List<GameObject>();
    Scene mainScene;

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        mainScene = SceneManager.GetActiveScene();
    }

    public void MovePlayer(Vector3 position, bool hasRotation, Vector3 rotation, bool hasSceneTransition, string sceneName, bool isGoingBackToMainScene)
    {
        StartCoroutine(MovePlayerCoroutine(position, hasRotation, rotation, hasSceneTransition, sceneName, isGoingBackToMainScene));
    }

    IEnumerator MovePlayerCoroutine(Vector3 position, bool hasRotation, Vector3 rotation, bool hasSceneTransition, string sceneName, bool isGoingBackToMainScene)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true; 
            yield return fadeCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();
        }

        transform.position = position;
        if (hasRotation) transform.rotation = Quaternion.Euler(rotation);

        if (hasSceneTransition)
        {
            if (isGoingBackToMainScene) StartCoroutine(TransitionBack(sceneName));
            else StartCoroutine(TransitionToSideScene(sceneName));
        }
        
        if (fadeCanvasGroup != null)
        {
            yield return fadeCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    IEnumerator TransitionToSideScene(string sceneName)
    {
        Scene primaryScene = SceneManager.GetActiveScene();

        primarySceneRoots.Clear();
        primarySceneRoots.AddRange(primaryScene.GetRootGameObjects());
        primarySceneRoots.Remove(objectToKeep);

        foreach (GameObject obj in hiddenObjects)
        {
            obj.SetActive(false);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return null; 

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        foreach (GameObject rootObj in primarySceneRoots)
        {
            rootObj.SetActive(false);
        }
    }

    IEnumerator TransitionBack(string sceneName)
    {
        SceneManager.SetActiveScene(mainScene);

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        foreach (GameObject rootObj in primarySceneRoots)
        {
            if (rootObj != null)
            {
                rootObj.SetActive(true);
            }
        }

        foreach (GameObject obj in hiddenObjects)
        {
            obj.SetActive(true);
        }
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class AddressableSceneLoader : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] GameObject loadingPanel;
    [SerializeField] Slider progressBar;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] TextMeshProUGUI percentText;

    [Header("Transition")]
    [SerializeField] SceneTransition sceneTransition;

    public static AsyncOperationHandle<SceneInstance> LastLoadedSceneHandle { get; set; }
    AsyncOperationHandle<SceneInstance> currentSceneHandle;

    void Start()
    {
        if (sceneTransition == null)
        {
            sceneTransition = GetComponentInChildren<SceneTransition>();
            if (sceneTransition == null) sceneTransition = FindAnyObjectByType<SceneTransition>();
        }
    }

    public void LoadAddressableScene(string sceneAddress)
    {
        StartCoroutine(LoadSceneCoroutine(sceneAddress));
    }

    IEnumerator LoadSceneCoroutine(string sceneAddress)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;

        // -------------------------------------------------------------
        // Step 1: Check how much data needs to be downloaded over HTTP
        // -------------------------------------------------------------
        var sizeHandle = Addressables.GetDownloadSizeAsync(sceneAddress);
        yield return sizeHandle;

        long downloadSizeBytes = sizeHandle.Result;
        Addressables.Release(sizeHandle);

        // -------------------------------------------------------------
        // Step 2: Download bundle dependencies if not already in browser cache
        // -------------------------------------------------------------
        if (downloadSizeBytes > 0)
        {
            float totalMB = downloadSizeBytes / (1024f * 1024f);
            var downloadHandle = Addressables.DownloadDependenciesAsync(sceneAddress, autoReleaseHandle: false);

            while (!downloadHandle.IsDone)
            {
                DownloadStatus status = downloadHandle.GetDownloadStatus();
                float progress = status.Percent; // 0.0 to 1.0
                float downloadedMB = status.DownloadedBytes / (1024f * 1024f);

                if (progressBar != null) progressBar.value = progress * 0.7f; // allocate first 70% to download
                if (statusText != null) statusText.text = $"Downloading: {downloadedMB:F1} MB / {totalMB:F1} MB";
                if (percentText != null) percentText.text = $"{Mathf.RoundToInt(progress * 70f)}%";

                yield return null;
            }

            Addressables.Release(downloadHandle);
        }

        // -------------------------------------------------------------
        // Step 3: Load the scene in the background
        // -------------------------------------------------------------
        if (statusText != null) statusText.text = "Initializing scene...";

        bool hadDownload = downloadSizeBytes > 0;
        var loadHandle = Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single, activateOnLoad: false);

        while (!loadHandle.IsDone)
        {
            float loadProgress = hadDownload 
                ? 0.7f + (loadHandle.PercentComplete * 0.3f) 
                : loadHandle.PercentComplete;

            if (progressBar != null) progressBar.value = loadProgress;
            if (percentText != null) percentText.text = $"{Mathf.RoundToInt(loadProgress * 100f)}%";

            yield return null;
        }

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            if (progressBar != null) progressBar.value = 1f;
            if (percentText != null) percentText.text = "100%";
            currentSceneHandle = loadHandle;
            LastLoadedSceneHandle = loadHandle;

            // Fade to black smoothly using SceneTransition before opening the new scene
            if (sceneTransition == null)
            {
                sceneTransition = GetComponentInChildren<SceneTransition>();
                if (sceneTransition == null) sceneTransition = FindAnyObjectByType<SceneTransition>();
            }

            if (sceneTransition != null)
            {
                yield return sceneTransition.Fade(1f, sceneTransition.FadeInDuration);
            }

            // Activate the new scene behind the black overlay
            yield return loadHandle.Result.ActivateAsync();
        }
        else
        {
            Debug.LogError($"[AddressableSceneLoader] Failed to load addressable scene '{sceneAddress}': {loadHandle.OperationException}");
            if (statusText != null) statusText.text = "Failed to load scene!";
        }
    }
}
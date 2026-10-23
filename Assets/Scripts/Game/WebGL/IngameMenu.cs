using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class IngameMenu : MonoBehaviour 
{
    [Header("UI")]
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] GameObject menuPanel;
    [SerializeField] Button toggleMenuButton;
    [SerializeField] Button resetViewButton;
    [SerializeField] Button returnToMenuButton;
    [SerializeField] SceneTransition sceneTransition;
    [SerializeField] string menuSceneName = "Menu_WebGL";

    public Slider sensSlider => sensitivitySlider;

    Vector3 initialCameraPosition;
    Quaternion initialCameraRotation;
    Transform camTransform;

    void OnEnable()
    {
        toggleMenuButton.onClick.AddListener(ToggleMenu);
        resetViewButton.onClick.AddListener(ResetCameraView);
        returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    void Start()
    {
        camTransform = Camera.main != null ? Camera.main.transform : transform;
        initialCameraPosition = camTransform.position;
        initialCameraRotation = camTransform.rotation;
    }

    void OnDisable()
    {
        toggleMenuButton.onClick.RemoveListener(ToggleMenu);
        resetViewButton.onClick.RemoveListener(ResetCameraView);
        returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
    }

    void ToggleMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }
    }

    void ResetCameraView()
    {
        if (camTransform != null)
        {
            camTransform.position = initialCameraPosition;
            camTransform.rotation = initialCameraRotation;
        }
    }

    public void ReturnToMenu()
    {
        string targetScene = string.IsNullOrEmpty(menuSceneName) ? "Menu_WebGL" : menuSceneName;

        if (sceneTransition != null)
        {
            sceneTransition.SwitchScene(targetScene);
        }
        else
        {
            StartCoroutine(ReturnToMenuRoutine(targetScene));
        }
    }

    IEnumerator ReturnToMenuRoutine(string targetScene)
    {
        if (PlayerCameraController.ActiveDioramaHandle.IsValid())
        {
            yield return Addressables.UnloadSceneAsync(PlayerCameraController.ActiveDioramaHandle);
            PlayerCameraController.ActiveDioramaHandle = default;
        }

        if (AddressableSceneLoader.LastLoadedSceneHandle.IsValid())
        {
            yield return Addressables.UnloadSceneAsync(AddressableSceneLoader.LastLoadedSceneHandle);
            AddressableSceneLoader.LastLoadedSceneHandle = default;
        }

        SceneManager.LoadScene(targetScene);
    }
}
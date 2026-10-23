using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class IngameMenu : MonoBehaviour 
{
    [Header("UI")]
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] GameObject controlPanel;
    [SerializeField] GameObject hintPanel;
    [SerializeField] GameObject controlButton;
    [SerializeField] GameObject hintButton;
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
        resetViewButton.onClick.AddListener(ResetCameraView);
        returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    void Start()
    {
        camTransform = Camera.main != null ? Camera.main.transform : transform;
        initialCameraPosition = camTransform.position;
        initialCameraRotation = camTransform.rotation;
    }

    void Update()
    {
        if (controlPanel.activeSelf) TurnOffMenu(controlPanel, controlButton);
        if (hintPanel.activeSelf) TurnOffMenu(hintPanel, hintButton);
    }

    void OnDisable()
    {
        resetViewButton.onClick.RemoveListener(ResetCameraView);
        returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
    }

    public void TurnOnControlPanel()
    {
        controlPanel.SetActive(true);
    }

    public void TurnOnHintPanel()
    {
        hintPanel.SetActive(true);
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

    void TurnOffMenu(GameObject panel, GameObject button)
    {
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (inputDetected)
        {
            if (EventSystem.current == null) return;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = inputPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool clickedInside = results.Exists(r => 
            r.gameObject == button|| 
            r.gameObject.transform.IsChildOf(panel.transform));

            if (!clickedInside)
            {
                panel.SetActive(false);
            }
        }
    }
}
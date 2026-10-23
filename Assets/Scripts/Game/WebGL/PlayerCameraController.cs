using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using DG.Tweening;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using NaughtyAttributes;

public class PlayerCameraController : MonoBehaviour
{
    public static PlayerCameraController Instance { get; private set; }

    [Header("Camera & Visuals")]
    [SerializeField] Camera targetCamera;
    [SerializeField] CanvasGroup fadeCanvasGroup;
    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] IngameMenu ingameMenu;

    [Header("Diorama / Scene Transition")]
    [SerializeField] GameObject objectToKeep;

    [Header("Input Action References (Desktop)")]
    [SerializeField] InputActionReference pointerDeltaAction;
    [SerializeField] InputActionReference orbitClickAction; 
    [SerializeField] InputActionReference panClickAction;   
    [SerializeField] InputActionReference scrollAction;

    [Header("Controls")]
    [SerializeField] bool pivotOnPointerRotate = true;
    [SerializeField] bool pivotOnPointerZoom = true;

    [Header("Sensitivities")]
    [SerializeField] float maxOrbitSensitivity = 0.1f;
    [SerializeField] float maxPanSensitivity = 0.2f;
    [SerializeField] float maxZoomSensitivity = 0.1f;
    [SerializeField] float maxMobileZoomSensitivity = 0.003f;

    [Header("Camera Limits")]
    [SerializeField] bool useCameraLimits = false;
    [SerializeField] float minX = -3.5f;
    [SerializeField] float maxX = 0f;
    [SerializeField] float minZ = -3f;
    [SerializeField] float maxZ = 0.35f;
    [SerializeField] float minY = 0.1f;
    [SerializeField] float maxY = 2f;

    [Header("Orbit & Pivot Settings")]
    [SerializeField] float defaultPivotDistance = 3f;
    [SerializeField] float minPivotDistance = 0.5f;
    [SerializeField] float maxPivotDistance = 15f;
    [SerializeField] float minPitch = -10f;
    [SerializeField] float maxPitch = 85f;
    [SerializeField] float groundY = 0f;

    [Header("Current Sensitivities (Read-Only)")]
    [ShowNonSerializedField] float orbitSensitivity = 0.055f;
    [ShowNonSerializedField] float panSensitivity = 0.105f;
    [ShowNonSerializedField] float zoomSensitivity = 0.055f;
    [ShowNonSerializedField] float mobileZoomSensitivity = 0.002f;

    Camera cam;
    Transform camTransform;
    Plane groundPlane;
    Slider sensitivitySlider;

    Vector3 activePivotPoint;
    bool isOrbiting = false;
    bool isPanning = false;
    bool wasOneTouch = false;
    bool wasTwoTouch = false;
    bool isTransitioning = false;

    List<GameObject> primarySceneRoots = new List<GameObject>();
    Scene mainScene;
    string additiveSceneName;
    public static AsyncOperationHandle<SceneInstance> ActiveDioramaHandle { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate PlayerCameraController detected on '{gameObject.name}'. Disabling duplicate.");
            enabled = false;
            return;
        }
        Instance = this;

        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) targetCamera = Camera.main;

        cam = targetCamera;
        camTransform = cam != null ? cam.transform : transform;
        groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));

        if (camTransform != null && (Mathf.Abs(camTransform.position.x) > 100f || Mathf.Abs(camTransform.position.z) > 100f))
        {
            Debug.LogWarning($"PlayerCameraController: Camera position {camTransform.position} is out of bounds! Resetting to default origin.");
            camTransform.position = new Vector3(-1.7f, 0.44f, 0.6f);
            camTransform.rotation = Quaternion.Euler(15f, 90f, 0f);
        }

        if (fadeCanvasGroup == null)
        {
            if (cam != null) fadeCanvasGroup = cam.GetComponentInChildren<CanvasGroup>();
            if (fadeCanvasGroup == null) fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        if (fadeCanvasGroup != null && !isTransitioning)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (objectToKeep == null)
        {
            GameObject gameplay = GameObject.Find("_Gameplay_");
            if (gameplay != null) objectToKeep = gameplay;
            else objectToKeep = transform.root.gameObject;
        }

        if (ingameMenu == null)
        {
            ingameMenu = FindAnyObjectByType<IngameMenu>();
        }

        if (ingameMenu != null && sensitivitySlider == null)
        {
            sensitivitySlider = ingameMenu.sensSlider;
        }

        activePivotPoint = GetPointOnGround(new Vector2(Screen.width / 2f, Screen.height / 2f));
        AdjustSensitivity();
    }

    void Start()
    {
        mainScene = SceneManager.GetActiveScene();

        if (ingameMenu == null)
        {
            ingameMenu = FindAnyObjectByType<IngameMenu>();
        }

        if (ingameMenu != null && sensitivitySlider == null)
        {
            sensitivitySlider = ingameMenu.sensSlider;
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        AdjustSensitivity();
    }

    void OnSensitivityChanged(float value)
    {
        AdjustSensitivity();
    }

    void OnDestroy()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable(); 
        
        if (pointerDeltaAction != null) pointerDeltaAction.action.Enable();
        if (orbitClickAction != null) orbitClickAction.action.Enable();
        if (panClickAction != null) panClickAction.action.Enable();
        if (scrollAction != null) scrollAction.action.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        
        if (pointerDeltaAction != null) pointerDeltaAction.action.Disable();
        if (orbitClickAction != null) orbitClickAction.action.Disable();
        if (panClickAction != null) panClickAction.action.Disable();
        if (scrollAction != null) scrollAction.action.Disable();
    }

    void LateUpdate()
    {
        if (cam == null || camTransform == null) return;
        if (isTransitioning) return;
        if (!isOrbiting && !isPanning && !wasOneTouch && !wasTwoTouch && IsPointerOverUI()) return;

        if (Touch.activeTouches.Count > 0)
        {
            isOrbiting = false;
            isPanning = false;

            if (Touch.activeTouches.Count == 1)
            {
                var touch = Touch.activeTouches[0];
                if (!wasOneTouch)
                {
                    Vector2 pos = pivotOnPointerRotate ? touch.screenPosition : new Vector2(Screen.width / 2f, Screen.height / 2f);
                    activePivotPoint = GetPointOnGround(pos);
                    wasOneTouch = true;
                }
                wasTwoTouch = false;

                HandleOrbit(touch.delta * orbitSensitivity, activePivotPoint);
            }
            else if (Touch.activeTouches.Count == 2)
            {
                var touch0 = Touch.activeTouches[0];
                var touch1 = Touch.activeTouches[1];

                if (!wasTwoTouch)
                {
                    Vector2 center = (touch0.screenPosition + touch1.screenPosition) / 2f;
                    Vector2 pos = pivotOnPointerRotate ? center : new Vector2(Screen.width / 2f, Screen.height / 2f);
                    activePivotPoint = GetPointOnGround(pos);
                    wasTwoTouch = true;
                }
                wasOneTouch = false;

                Vector2 dir0 = touch0.delta.normalized;
                Vector2 dir1 = touch1.delta.normalized;
                float swipeAlignment = Vector2.Dot(dir0, dir1);

                if (swipeAlignment > 0.6f)
                {
                    Vector2 averageDelta = (touch0.delta + touch1.delta) / 2f;
                    HandlePan(averageDelta * panSensitivity * 3, activePivotPoint);
                }
                else if (swipeAlignment < 0.3f)
                {
                    HandleMobileZoom(touch0, touch1);
                }
            }
            else
            {
                wasOneTouch = false;
                wasTwoTouch = false;
            }
        }
        else
        {
            wasOneTouch = false;
            wasTwoTouch = false;

            Vector2 pointerDelta = pointerDeltaAction != null ? pointerDeltaAction.action.ReadValue<Vector2>() : Vector2.zero;
            
            bool orbitInProgress = orbitClickAction != null && orbitClickAction.action.IsInProgress();
            if (orbitInProgress && !isOrbiting)
            {
                Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
                Vector2 pos = pivotOnPointerRotate ? screenPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
                activePivotPoint = GetPointOnGround(pos);
                isOrbiting = true;
            }
            else if (!orbitInProgress)
            {
                isOrbiting = false;
            }

            bool panInProgress = panClickAction != null && panClickAction.action.IsInProgress();
            if (panInProgress && !isPanning)
            {
                Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
                Vector2 pos = pivotOnPointerRotate ? screenPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
                activePivotPoint = GetPointOnGround(pos);
                isPanning = true;
            }
            else if (!panInProgress)
            {
                isPanning = false;
            }
            
            if (orbitInProgress)
            {
                HandleOrbit(pointerDelta * orbitSensitivity, activePivotPoint);
            }
            else if (panInProgress)
            {
                HandlePan(pointerDelta * panSensitivity, activePivotPoint);
            }

            HandleDesktopZoom();
        }

        if (useCameraLimits)
        {
            camTransform.position = new Vector3(
                Mathf.Clamp(camTransform.position.x, minX, maxX),
                Mathf.Clamp(camTransform.position.y, minY, maxY),
                Mathf.Clamp(camTransform.position.z, minZ, maxZ)
            );
        }
        else
        {
            // Global safety limits to prevent camera ever flying into outer space
            camTransform.position = new Vector3(
                Mathf.Clamp(camTransform.position.x, -50f, 50f),
                Mathf.Clamp(camTransform.position.y, -10f, 30f),
                Mathf.Clamp(camTransform.position.z, -50f, 50f)
            );
        }
    }

    Vector3 GetPointOnGround(Vector2 screenPos)
    {
        Vector3 fallbackPivot = camTransform != null 
            ? camTransform.position + camTransform.forward * defaultPivotDistance 
            : transform.position + transform.forward * defaultPivotDistance;

        if (cam == null) return fallbackPivot;

        Ray ray = cam.ScreenPointToRay(screenPos);
        // Only raycast towards ground plane if the ray is pointing downwards
        if (ray.direction.y < -0.05f && groundPlane.Raycast(ray, out float distanceToPlane))
        {
            if (distanceToPlane >= minPivotDistance && distanceToPlane <= maxPivotDistance)
            {
                return ray.GetPoint(distanceToPlane);
            }
        }

        return fallbackPivot;
    }

    void HandleOrbit(Vector2 delta, Vector3 pivot)
    {
        // Horizontal (Yaw) rotation around pivot
        camTransform.RotateAround(pivot, Vector3.up, delta.x);

        // Vertical (Pitch) rotation clamped to prevent flipping or horizon locking
        float currentPitch = camTransform.eulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        
        float pitchChange = -delta.y;
        float targetPitch = Mathf.Clamp(currentPitch + pitchChange, minPitch, maxPitch);
        float actualPitchChange = targetPitch - currentPitch;

        if (Mathf.Abs(actualPitchChange) > 0.0001f)
        {
            camTransform.RotateAround(pivot, camTransform.right, actualPitchChange);
        }
    }

    void HandlePan(Vector2 delta, Vector3 pivot)
    {
        float distanceToPivot = Vector3.Distance(camTransform.position, pivot);
        distanceToPivot = Mathf.Clamp(distanceToPivot, minPivotDistance, maxPivotDistance);
        float speedMultiplier = distanceToPivot * 0.002f;

        Vector3 rightMovement = camTransform.right * -delta.x;
        Vector3 forwardFlat = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;
        Vector3 forwardMovement = forwardFlat * -delta.y;

        Vector3 panMove = (rightMovement + forwardMovement) * speedMultiplier;
        camTransform.position += panMove;
    }

    void HandleDesktopZoom()
    {
        if (scrollAction == null) return;
        float scroll = scrollAction.action.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 pos = pivotOnPointerZoom ? screenPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = cam.ScreenPointToRay(pos);
            camTransform.position += ray.direction * Mathf.Sign(scroll) * zoomSensitivity;
        }
    }

    void HandleMobileZoom(Touch touch0, Touch touch1)
    {
        float currentDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
        float previousDistance = Vector2.Distance(
            touch0.screenPosition - touch0.delta, 
            touch1.screenPosition - touch1.delta);

        float pinchDelta = currentDistance - previousDistance;
        
        Vector2 centerPosition = (touch0.screenPosition + touch1.screenPosition) / 2f;
        Vector2 pos = pivotOnPointerZoom ? centerPosition : new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = cam.ScreenPointToRay(pos);
        
        camTransform.position += ray.direction * pinchDelta * mobileZoomSensitivity;
    }

    #region Sequence & Player Transition (VR Replacement)

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        if (camTransform != null)
        {
            camTransform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            transform.SetPositionAndRotation(position, rotation);
        }
        activePivotPoint = GetPointOnGround(new Vector2(Screen.width / 2f, Screen.height / 2f));
    }

    public void MovePlayer(Vector3 position, bool hasRotation, Vector3 rotation, bool hasSceneTransition, string sceneName, bool isGoingBackToMainScene)
    {
        StartCoroutine(MovePlayerCoroutine(position, hasRotation, rotation, hasSceneTransition, sceneName, isGoingBackToMainScene));
    }

    IEnumerator MovePlayerCoroutine(Vector3 position, bool hasRotation, Vector3 rotation, bool hasSceneTransition, string sceneName, bool isGoingBackToMainScene)
    {
        isTransitioning = true;
        isOrbiting = false;
        isPanning = false;
        wasOneTouch = false;
        wasTwoTouch = false;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true; 
            yield return fadeCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();
        }

        if (camTransform != null)
        {
            camTransform.position = position;
            if (hasRotation) camTransform.rotation = Quaternion.Euler(rotation);
        }
        else
        {
            transform.position = position;
            if (hasRotation) transform.rotation = Quaternion.Euler(rotation);
        }

        if (hasSceneTransition)
        {
            if (isGoingBackToMainScene) yield return TransitionBack(sceneName);
            else yield return TransitionToSideScene(sceneName);
        }

        activePivotPoint = GetPointOnGround(new Vector2(Screen.width / 2f, Screen.height / 2f));

        yield return new WaitForSeconds(1f);
        
        if (fadeCanvasGroup != null)
        {
            yield return fadeCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();
            fadeCanvasGroup.blocksRaycasts = false;
        }

        isTransitioning = false;
    }

    IEnumerator TransitionToSideScene(string sceneName)
    {
        Scene primaryScene = SceneManager.GetActiveScene();
        additiveSceneName = sceneName;

        primarySceneRoots.Clear();
        primarySceneRoots.AddRange(primaryScene.GetRootGameObjects());
        
        if (objectToKeep != null)
        {
            primarySceneRoots.Remove(objectToKeep);
        }
        else
        {
            primarySceneRoots.Remove(transform.root.gameObject);
            if (camTransform != null) primarySceneRoots.Remove(camTransform.root.gameObject);
        }

        LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;

        // Try loading additively via Addressables first
        bool loadedViaAddressables = false;
        AsyncOperationHandle<SceneInstance> loadHandle = default;

        try
        {
            loadHandle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive, activateOnLoad: true);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayerCameraController] Addressables.LoadSceneAsync exception for '{sceneName}': {ex.Message}");
        }

        if (loadHandle.IsValid())
        {
            yield return loadHandle;
            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                ActiveDioramaHandle = loadHandle;
                loadedViaAddressables = true;
                SceneManager.SetActiveScene(loadHandle.Result.Scene);
            }
            else
            {
                Debug.LogWarning($"[PlayerCameraController] Addressable scene '{sceneName}' failed to load ({loadHandle.OperationException}). Falling back to SceneManager.LoadSceneAsync...");
                Addressables.Release(loadHandle);
            }
        }

        // Fallback to standard SceneManager if not registered in Addressables
        if (!loadedViaAddressables)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return null; 
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        }

        foreach (GameObject rootObj in primarySceneRoots)
        {
            if (rootObj != null) rootObj.SetActive(false);
        }
    }

    IEnumerator TransitionBack(string sceneName)
    {
        SceneManager.SetActiveScene(mainScene);

        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

        // Unload Diorama via Addressables (frees AssetBundle & memory) or fallback to SceneManager
        if (ActiveDioramaHandle.IsValid())
        {
            yield return Addressables.UnloadSceneAsync(ActiveDioramaHandle);
            ActiveDioramaHandle = default;
        }
        else
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
            while (!asyncUnload.isDone)
            {
                yield return null;
            }
        }

        foreach (GameObject rootObj in primarySceneRoots)
        {
            if (rootObj != null)
            {
                rootObj.SetActive(true);
            }
        }

        additiveSceneName = null;
        Debug.Log("Back to main scene");
    }

    public void LoadNormalSceneFadeOut(string sceneName)
    {
        StartCoroutine(LoadNormalScene(sceneName));
        Debug.Log("LoadNormalSceneFadeOut: " + sceneName);
    }

    public void ReturnToBootstrapMenu()
    {
        LoadNormalSceneFadeOut("Menu_WebGL");
    }

    IEnumerator LoadNormalScene(string sceneName)
    {
        if (additiveSceneName != null)
        {
            if (ActiveDioramaHandle.IsValid())
            {
                yield return Addressables.UnloadSceneAsync(ActiveDioramaHandle);
                ActiveDioramaHandle = default;
            }
            else
            {
                yield return SceneManager.UnloadSceneAsync(additiveSceneName);
            }
            additiveSceneName = null;
        }

        Debug.Log("Loading new scene...");

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return fadeCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();
        }

        // Release Addressable bundle memory of current main scene if loaded via AddressableSceneLoader
        if (AddressableSceneLoader.LastLoadedSceneHandle.IsValid())
        {
            yield return Addressables.UnloadSceneAsync(AddressableSceneLoader.LastLoadedSceneHandle);
            AddressableSceneLoader.LastLoadedSceneHandle = default;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            yield return SceneManager.LoadSceneAsync(0);
        }
        else
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
        }
    }
    #endregion

    #region UI & Helpers

    public void AdjustSensitivity()
    {
        float sliderValue = sensitivitySlider != null ? sensitivitySlider.value : 0.5f;
        orbitSensitivity = Mathf.Lerp(0.01f, maxOrbitSensitivity, sliderValue);
        panSensitivity = Mathf.Lerp(0.01f, maxPanSensitivity, sliderValue);
        zoomSensitivity = Mathf.Lerp(0.01f, maxZoomSensitivity, sliderValue);
        mobileZoomSensitivity = Mathf.Lerp(0.001f, maxMobileZoomSensitivity, sliderValue);
    }

    public void SetLanguage(string lang)
    {
        LocalizationManager.Instance.SetLanguage(lang);
    }

    public void EnableCameraLimits(bool enable)
    {
        useCameraLimits = enable;
    }

    public void SetCameraLimits(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
        this.minZ = minZ;
        this.maxZ = maxZ;
        useCameraLimits = true;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        for (int i = 0; i < Touch.activeTouches.Count; i++)
        {
            Touch touch = Touch.activeTouches[i];
            if (EventSystem.current.IsPointerOverGameObject(touch.touchId))
            {
                return true;
            }
        }
        return false;
    }

    #endregion
}
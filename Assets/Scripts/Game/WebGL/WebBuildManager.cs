using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class WebBuildManager : MonoBehaviour
{
    [Header("Input Action References (Desktop)")]
    [SerializeField] InputActionReference pointerDeltaAction;
    [SerializeField] InputActionReference orbitClickAction; 
    [SerializeField] InputActionReference panClickAction;   
    [SerializeField] InputActionReference scrollAction;

    [Header("Sensitivities")]
    [SerializeField] float orbitSensitivity = 0.5f;
    [SerializeField] float panSensitivity = 0.5f;
    [SerializeField] float zoomSensitivity = 2f;
    [SerializeField] float mobileZoomSensitivity = 0.01f;

    [Header("UI")]
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] Button frButton, enButton, vnButton;
    [SerializeField] GameObject menuPanel;
    [SerializeField] Button toggleMenuButton;

    [Header("Camera Limits")]
    [SerializeField] float minX = -3.5f;
    [SerializeField] float maxX = 0f;
    [SerializeField] float minZ = -3f;
    [SerializeField] float maxZ = 0.35f;
    [SerializeField] float minY = 0.1f;
    [SerializeField] float maxY = 2f;

    Camera cam;
    Transform camTransform;
    Plane groundPlane;

    Vector3 activePivotPoint;
    bool isOrbiting = false;
    bool isPanning = false;
    bool wasOneTouch = false;
    bool wasTwoTouch = false;

    void Awake()
    {
        cam = Camera.main;
        camTransform = cam.transform;
        groundPlane = new Plane(Vector3.up, Vector3.zero);
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable(); 
        
        pointerDeltaAction.action.Enable();
        orbitClickAction.action.Enable();
        panClickAction.action.Enable();
        scrollAction.action.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        
        pointerDeltaAction.action.Disable();
        orbitClickAction.action.Disable();
        panClickAction.action.Disable();
        scrollAction.action.Disable();
    }

    void Start()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener((value) => AdjustSensitivity());
            AdjustSensitivity(); 
        }

        if (frButton != null) frButton.onClick.AddListener(() => SetLanguage("French"));
        if (enButton != null) enButton.onClick.AddListener(() => SetLanguage("English"));
        if (vnButton != null) vnButton.onClick.AddListener(() => SetLanguage("Vietnamese"));

        if (toggleMenuButton != null) toggleMenuButton.onClick.AddListener(() => ToggleMenu());
    }

    void LateUpdate()
    {
        if (IsPointerOverUI())
        {
            isOrbiting = false;
            isPanning = false;
            wasOneTouch = false;
            wasTwoTouch = false;
            return;
        }

        if (Touch.activeTouches.Count > 0)
        {
            isOrbiting = false;
            isPanning = false;

            if (Touch.activeTouches.Count == 1)
            {
                var touch = Touch.activeTouches[0];
                if (!wasOneTouch)
                {
                    activePivotPoint = GetPointOnGround(touch.screenPosition);
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
                    activePivotPoint = GetPointOnGround(center);
                    wasTwoTouch = true;
                }
                wasOneTouch = false;

                // Normalize deltas to get pure direction, ignoring how fast they are swiping
                Vector2 dir0 = touch0.delta.normalized;
                Vector2 dir1 = touch1.delta.normalized;

                // Calculate alignment: 1 (parallel), 0 (perpendicular), -1 (opposite)
                float swipeAlignment = Vector2.Dot(dir0, dir1);

                // If both fingers are moving in roughly the same direction (e.g., > 0.6)
                if (swipeAlignment > 0.6f)
                {
                    // It's a PAN. Calculate average delta and ignore zoom.
                    Vector2 averageDelta = (touch0.delta + touch1.delta) / 2f;
                    HandlePan(averageDelta * panSensitivity, activePivotPoint);
                }
                // If fingers are moving in opposite directions, or one is held still (< 0.3)
                else if (swipeAlignment < 0.3f)
                {
                    // It's a ZOOM. Ignore pan.
                    HandleMobileZoom(touch0, touch1);
                }
                // If it falls between 0.3 and 0.6, it's a deadzone to prevent jitter
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

            Vector2 pointerDelta = pointerDeltaAction.action.ReadValue<Vector2>();
            
            bool orbitInProgress = orbitClickAction.action.IsInProgress();
            if (orbitInProgress && !isOrbiting)
            {
                Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
                activePivotPoint = GetPointOnGround(screenPos);
                isOrbiting = true;
            }
            else if (!orbitInProgress)
            {
                isOrbiting = false;
            }

            bool panInProgress = panClickAction.action.IsInProgress();
            if (panInProgress && !isPanning)
            {
                Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
                activePivotPoint = GetPointOnGround(screenPos);
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

        camTransform.position = new Vector3(
            Mathf.Clamp(camTransform.position.x, minX, maxX),
            Mathf.Clamp(camTransform.position.y, minY, maxY),
            Mathf.Clamp(camTransform.position.z, minZ, maxZ)
        );
    }

    Vector3 GetPointOnGround(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (groundPlane.Raycast(ray, out float distanceToPlane))
        {
            return ray.GetPoint(distanceToPlane);
        }
        return camTransform.position + camTransform.forward * 50f;
    }

    void HandleOrbit(Vector2 delta, Vector3 pivot)
    {
        camTransform.RotateAround(pivot, Vector3.up, delta.x);

        float currentPitch = camTransform.eulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        
        float pitchChange = -delta.y;
        float nextPitch = currentPitch + pitchChange;

        if (nextPitch > 5f && nextPitch < 85f)
        {
            camTransform.RotateAround(pivot, camTransform.right, pitchChange);
        }
    }

    void HandlePan(Vector2 delta, Vector3 pivot)
    {
        float distanceToPivot = Vector3.Distance(camTransform.position, pivot);
        float speedMultiplier = distanceToPivot * 0.002f;

        Vector3 rightMovement = camTransform.right * -delta.x;
        Vector3 forwardFlat = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;
        Vector3 forwardMovement = forwardFlat * -delta.y;

        Vector3 panMove = (rightMovement + forwardMovement) * speedMultiplier;
        
        camTransform.position += panMove;
    }

    void HandleDesktopZoom()
    {
        float scroll = scrollAction.action.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = cam.ScreenPointToRay(screenPos);
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
        Ray ray = cam.ScreenPointToRay(centerPosition);
        
        camTransform.position += ray.direction * pinchDelta * mobileZoomSensitivity;
    }

    void AdjustSensitivity()
    {
        float sliderValue = sensitivitySlider.value;
        orbitSensitivity = Mathf.Lerp(0.01f, 0.4f, sliderValue);
        panSensitivity = Mathf.Lerp(0.01f, 0.4f, sliderValue);
        zoomSensitivity = Mathf.Lerp(0.01f, 0.1f, sliderValue);
        mobileZoomSensitivity = Mathf.Lerp(0.001f, 0.01f, sliderValue);
    }

    void SetLanguage(string lang)
    {
        LocalizationManager.Instance.SetLanguage(lang);
    }

    bool IsPointerOverUI()
    {
        // 1. Check for Mouse (PC/Editor)
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        // 2. Check for Mobile Touches
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

    void ToggleMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }
    }
}
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WebPOIManager : MonoBehaviour 
{
    public static WebPOIManager Instance { get; private set; }

    [Header("Range Thresholds")]
    [SerializeField] float rangeSign = 10f; 
    [SerializeField] float viewDotThreshold = 0.5f;

    [Header("Transition durations")]
    [SerializeField] float popDuration = 0.5f;
    [SerializeField] float fadeDuration = 0.2f;

    [Header("Images Display")]
    [SerializeField] float imageDisplayDotThreshold = 0.3f;
    [SerializeField] GameObject imageDisplay;
    [SerializeField] Image image;
    [SerializeField] TextMeshProUGUI descriptionText;
    
    List<PointOfInterest> poiList = new List<PointOfInterest>();
    Transform camTransform;
    Transform currentPOITransform;
    PointOfInterest displayingPOI;
    MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        camTransform = Camera.main != null ? Camera.main.transform : null;
        propertyBlock = new MaterialPropertyBlock();

        // Find all POIs in the scene
        PointOfInterest[] allPOIs = FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        poiList.AddRange(allPOIs);

        // FIX FOR WORLD SPACE CANVAS CLICKS:
        // Automatically assign the main camera as the Event Camera for all POI canvases
        foreach (var poi in poiList)
        {
            if (poi != null && poi.canvas != null)
            {
                Canvas poiCanvas = poi.canvas.GetComponent<Canvas>();
                if (poiCanvas != null && poiCanvas.renderMode == RenderMode.WorldSpace)
                {
                    poiCanvas.worldCamera = Camera.main;
                    
                    // A GraphicRaycaster is required for EventSystem to hit UI elements
                    if (poiCanvas.GetComponent<GraphicRaycaster>() == null)
                    {
                        poiCanvas.gameObject.AddComponent<GraphicRaycaster>();
                    }
                }
            }
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < poiList.Count; i++)
        {
            if (poiList[i] != null)
                poiList[i].ChangeState(0, popDuration, fadeDuration);
        }
    }

    void Update()
    {
        if (camTransform == null)
        {
            if (Camera.main != null) camTransform = Camera.main.transform;
            else return;
        }

        Vector3 camPos = camTransform.position;
        Vector3 camForward = camTransform.forward;

        // Process distance based popping for signs (Only State 0 [Hidden] and State 2 [Details])
        for (int i = 0; i < poiList.Count; i++)
        {
            if (poiList[i] != null)
            {
                ProcessCanvas(poiList[i], camPos, camForward);
            }
        }

        CheckIfDisplayROIOutOfRange();
        HandleInput();
    }

    void ProcessCanvas(PointOfInterest poi, Vector3 camPos, Vector3 camForward)
    {
        Vector3 dirToTarget = poi.transform.position - camPos;
        float dot = Vector3.Dot(camForward, dirToTarget.normalized);
        
        if (dot < viewDotThreshold)
        {
            poi.ChangeState(0, popDuration, fadeDuration);
            return;
        }

        float headSqrDistance = dirToTarget.sqrMagnitude;

        // In WebGL, only toggle between State 0 (Hidden) and State 2 (Details)
        if (headSqrDistance > rangeSign * rangeSign)
        {
            poi.ChangeState(0, popDuration, fadeDuration);
        }
        else
        {
            poi.ChangeState(2, popDuration, fadeDuration);
        }
    }

    void HandleInput()
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

        // Detect click/touch interaction using EventSystem
        if (inputDetected)
        {
            if (EventSystem.current == null) return;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = inputPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            PointOfInterest clickedPOI = null;
            bool clickedImageDisplay = false;

            foreach (RaycastResult result in results)
            {
                GameObject hitObj = result.gameObject;

                // Check if user clicked on the imageDisplay picture UI
                if (imageDisplay != null && hitObj.transform.IsChildOf(imageDisplay.transform))
                {
                    clickedImageDisplay = true;
                    break;
                }

                // Check if user clicked on a POI sign icon
                PointOfInterest poi = hitObj.GetComponentInParent<PointOfInterest>();
                if (poi != null && poi.currentState != 0)
                {
                    // Confirm the hit object is part of the sign UI
                    if (poi.sign != null && hitObj.transform.IsChildOf(poi.details.transform))
                    {
                        clickedPOI = poi;
                        break;
                    }
                }
            }

            if (clickedPOI != null)
            {
                // Clicked on a sign icon -> Display information for that POI
                SetDisplayPOI(clickedPOI);
            }
            else if (!clickedImageDisplay && displayingPOI != null)
            {
                // Clicked outside the picture and not on another sign icon -> Reset/Close display
                ResetDisplayPOI(null);
                CloseDisplayUI();
            }
        }
    }

    public void SetDisplayPOI(PointOfInterest poi)
    {
        ResetDisplayPOI(poi);
        
        if (imageDisplay != null) imageDisplay.SetActive(true);

        poi.SetHighlight(propertyBlock, 1f);

        if (image != null && poi.displayTexture != null)
        {
            image.sprite = poi.displayTexture;
        }

        if (descriptionText != null && poi.TryGetComponent<LocalizedKey>(out LocalizedKey localizedKey))
        {
            string key = localizedKey.localizationKey;
            descriptionText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetLocalizedValue(key) : key;
        }
    }

    void ResetDisplayPOI(PointOfInterest poi)
    {
        if (displayingPOI != poi)
        {
            if (displayingPOI != null) 
            {
                displayingPOI.SetHighlight(propertyBlock, 0f);
            }
        }

        displayingPOI = poi;
        currentPOITransform = poi != null ? poi.transform : null;
    }

    void CloseDisplayUI()
    {
        if (image != null) image.sprite = null;
        if (imageDisplay != null) imageDisplay.SetActive(false);
    }

    void CheckIfDisplayROIOutOfRange()
    {
        if (displayingPOI != null && currentPOITransform != null && imageDisplay != null && imageDisplay.activeSelf)
        {
            Vector3 direction = currentPOITransform.position - camTransform.position;
            float dot = Vector3.Dot(camTransform.forward, direction.normalized);
            float sqrDist = direction.sqrMagnitude;
            
            // Close display if looking too far away or out of range
            if (dot < imageDisplayDotThreshold || sqrDist > (rangeSign * 1.5f) * (rangeSign * 1.5f))
            {
                ResetDisplayPOI(null);
                CloseDisplayUI();
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class POIManager : MonoBehaviour 
{
    public static POIManager Instance { get; private set; }

    [Header("Range Thresholds")]
    [SerializeField] float rangeSign = 1f; 
    [SerializeField] float rangeDetails = 0.5f;
    [SerializeField] float viewDotThreshold = 0.7f;

    [Header("Finger tips transform")]
    [SerializeField] Transform leftFingerTip;
    [SerializeField] Transform rightFingerTip;
    [SerializeField] float fingerDirectionDotThreshold = 0.85f;

    [Header("Transition durations")]
    [SerializeField] float popDuration = 0.5f;
    [SerializeField] float fadeDuration = 0.2f;

    [Header("Images Display")]
    [SerializeField] GameObject imageDisplay;
    [SerializeField] Image image;
    [SerializeField] string propertyName;

    static readonly List<PointOfInterest> poiList = new List<PointOfInterest>();
    Transform camTransform;
    PointOfInterest displayingPOI;
    MaterialPropertyBlock propertyBlock;
    int propertyID;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        camTransform = Camera.main.transform;

        propertyBlock = new MaterialPropertyBlock();
        propertyID = Shader.PropertyToID(propertyName);

    }

    void OnDisable()
    {
        for (int i = 0; i < poiList.Count; i++)
        {
            poiList[i].ChangeState(0, popDuration, fadeDuration);
        }
    }

    public static void Register(PointOfInterest poi)
    {
        if (!poiList.Contains(poi))
        {
            poiList.Add(poi);
        }
    }

    public static void Unregister(PointOfInterest poi)
    {
        poiList.Remove(poi);
    }

    void Update()
    {
        Vector3 camPos = camTransform.position;
        Vector3 camForward = camTransform.forward;

        for (int i = 0; i < poiList.Count; i++)
        {
            ProcessCanvas(poiList[i], camPos, camForward, leftFingerTip, rightFingerTip);
        }

        CheckIfDisplayROIOutOfRange();
    }

    void ProcessCanvas(PointOfInterest poi, Vector3 camPos, Vector3 camForward, Transform leftFingerTip, Transform rightFingerTip)
    {
        Vector3 dirToTarget = poi.transform.position - camPos;
        float dot = Vector3.Dot(camForward, dirToTarget.normalized);
        
        if (dot < viewDotThreshold)
        {
            poi.ChangeState(0, popDuration, fadeDuration);
            return;
        }

        float headSqrDistance = dirToTarget.sqrMagnitude;

        if (headSqrDistance > rangeSign * rangeSign)
        {
            poi.ChangeState(0, popDuration, fadeDuration);
        }
        else if (headSqrDistance <= rangeDetails * rangeDetails)
        {
            poi.ChangeState(2, popDuration, fadeDuration);
        }
        else
        {
            if (IsPointingAt(leftFingerTip, poi.transform.position) || IsPointingAt(rightFingerTip, poi.transform.position))
            {
                poi.ChangeState(2, popDuration, fadeDuration);
            }
            else
            {
                poi.ChangeState(1, popDuration, fadeDuration);
            }
        }
    }

    bool IsPointingAt(Transform fingerTip, Vector3 destination)
    {
        bool active = fingerTip != null && fingerTip.gameObject.activeInHierarchy;
        if (!active) return false;

        Vector3 fingerTipPos = fingerTip.position;
        float sqrDistance = (fingerTipPos - destination).sqrMagnitude;
        if (sqrDistance > rangeDetails * rangeDetails) return false;

        Vector3 direction = destination - fingerTipPos;
        float dot = Vector3.Dot(fingerTip.forward, direction.normalized);
        return dot > fingerDirectionDotThreshold;
    }

    public void SetDisplayPOI(PointOfInterest poi)
    {
        ResetDisplayPOI(poi);
        imageDisplay.SetActive(true);

        MeshRenderer[] renderers = poi.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length > 0)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(propertyID, 1f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        if (poi.displayTexture != null)
        {
            image.sprite = poi.displayTexture;
        }
    }

    void ResetDisplayPOI(PointOfInterest poi)
    {
        if (displayingPOI != poi)
        {
            Debug.Log("ResetDisplayPOI: " + (displayingPOI != null ? displayingPOI.name : "null") + " -> " + (poi != null ? poi.name : "null"));
            if (displayingPOI != null) 
            {
                MeshRenderer[] renderers = displayingPOI.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in renderers)
                {
                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat(propertyID, 0f);
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        displayingPOI = poi;
    }

    void CheckIfDisplayROIOutOfRange()
    {
        if (displayingPOI != null)
        {
            if (displayingPOI.currentState == 0)
            {
                ResetDisplayPOI(null);
                image.sprite = null;
                imageDisplay.SetActive(false);
            }
        }
    }
}

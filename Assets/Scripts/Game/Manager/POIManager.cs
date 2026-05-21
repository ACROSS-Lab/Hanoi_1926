using System.Collections.Generic;
using UnityEngine;

public class POIManager : MonoBehaviour 
{
    public static POIManager Instance { get; private set; }

    [Header("Range Thresholds")]
    [SerializeField] float rangeSign = 1f, rangeDetails = 0.5f;
    [SerializeField] float viewDotThreshold = 0.7f;

    [Header("Finger tips transform")]
    [SerializeField] Transform leftFingerTip;
    [SerializeField] Transform rightFingerTip;

    [Header("Transition durations")]
    [SerializeField] float popDuration = 0.5f;
    [SerializeField] float fadeDuration = 0.2f;

    bool leftActive, rightActive;

    static readonly List<PointOfInterest> poiList = new List<PointOfInterest>();
    Transform camTransform;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        camTransform = Camera.main.transform;
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

        leftActive = leftFingerTip != null && leftFingerTip.gameObject.activeInHierarchy;
        rightActive = rightFingerTip != null && rightFingerTip.gameObject.activeInHierarchy;

        Vector3 leftPos = leftActive ? leftFingerTip.position : Vector3.positiveInfinity;
        Vector3 rightPos = rightActive ? rightFingerTip.position : Vector3.positiveInfinity;

        for (int i = 0; i < poiList.Count; i++)
        {
            ProcessCanvas(poiList[i], camPos, camForward, leftPos, rightPos);
        }
    }

    void ProcessCanvas(PointOfInterest poi, Vector3 camPos, Vector3 camForward, Vector3 leftPos, Vector3 rightPos)
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
            float leftSqrDistance = (leftPos - poi.transform.position).sqrMagnitude;
            float rightSqrDistance = (rightPos - poi.transform.position).sqrMagnitude;

            if (leftSqrDistance < rangeDetails * rangeDetails || rightSqrDistance < rangeDetails * rangeDetails)
            {
                poi.ChangeState(2, popDuration, fadeDuration);
            }
            else
            {
                poi.ChangeState(1, popDuration, fadeDuration);
            }
        }
    }
}

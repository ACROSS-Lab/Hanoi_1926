using System.Collections.Generic;
using UnityEngine;

public class POIManager : MonoBehaviour 
{
    public static POIManager Instance { get; private set; }

    [SerializeField] float rangeSign = 1f, rangeDetails = 0.5f;
    float viewDotThreshold = 0.7f;

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
            ChangeState(poiList[i], 0);
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
            ProcessCanvas(poiList[i], camPos, camForward);
        }
    }

    void ProcessCanvas(PointOfInterest poi, Vector3 camPos, Vector3 camForward)
    {
        Vector3 dirToTarget = poi.transform.position - camPos;
        
        float dot = Vector3.Dot(camForward, dirToTarget.normalized);
        
        if (dot < viewDotThreshold)
        {
            ChangeState(poi, 0);
            return;
        }

        float sqrDistance = dirToTarget.sqrMagnitude;

        if (sqrDistance <= (rangeDetails * rangeDetails))
        {
            ChangeState(poi, 2);
            RotateCanvasTowardsCamera(poi);
        }
        else if (sqrDistance <= (rangeSign * rangeSign))
        {
            ChangeState(poi, 1);
            RotateCanvasTowardsCamera(poi);
        }
        else
        {
            ChangeState(poi, 0);
        }
    }

    void ChangeState(PointOfInterest poi, int newState)
    {
        if (poi.currentState == newState) return;

        poi.currentState = newState;

        switch (newState)
        {
            case 0: 
                if (poi.sign.activeSelf) poi.sign.SetActive(false);
                if (poi.details.activeSelf) poi.details.SetActive(false);
                break;
            case 1: 
                if (!poi.sign.activeSelf) poi.sign.SetActive(true);
                if (poi.details.activeSelf) poi.details.SetActive(false);
                break;
            case 2: 
                if (poi.sign.activeSelf) poi.sign.SetActive(false);
                if (!poi.details.activeSelf) poi.details.SetActive(true);
                break;
        }
    }

    void RotateCanvasTowardsCamera(PointOfInterest poi)
    {
        GameObject canvas = poi.canvas;
        Vector3 directionToCamera = camTransform.position - canvas.transform.position;
        canvas.transform.rotation = Quaternion.LookRotation(directionToCamera);
    }
}

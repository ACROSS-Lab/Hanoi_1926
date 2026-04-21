using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

public class HandGestureDetection : MonoBehaviour 
{
    [SerializeField] XRHandTrackingEvents handTracking;
    [SerializeField] XRHandPose handPose;
    [SerializeField] float minimumHoldTime = 0.2f;
    [SerializeField] float intervalTime = 0.05f;
    [SerializeField] UnityEvent gestureStarted;
    [SerializeField] UnityEvent gestureEnded;

    bool wasDetected, performedTriggered;
    float lastTimeCheck, holdStartTime;

    void OnEnable() 
    {
        handTracking.jointsUpdated.AddListener(OnJointsUpdated);
        // CRITICAL: Catch when the hand completely leaves camera view
        handTracking.trackingLost.AddListener(OnTrackingLost); 
    }

    void OnDisable() 
    {
        handTracking.jointsUpdated.RemoveListener(OnJointsUpdated);
        handTracking.trackingLost.RemoveListener(OnTrackingLost);
        
        // Safety check: If the script is turned off mid-gesture, force it to end.
        if (performedTriggered)
        {
            gestureEnded?.Invoke();
            performedTriggered = false;
            wasDetected = false;
        }
    }

    void OnTrackingLost()
    {
        // If the hand disappears, immediately cancel everything
        if (wasDetected)
        {
            wasDetected = false;
            if (performedTriggered)
            {
                gestureEnded?.Invoke();
                performedTriggered = false;
            }
        }
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
    {
        if (!isActiveAndEnabled || Time.time < lastTimeCheck + intervalTime) return;

        bool detected = handTracking.handIsTracked && handPose.CheckConditions(eventArgs);

        if (!wasDetected && detected)
        {
            holdStartTime = Time.time;
        }
        else if (wasDetected && !detected)
        {
            // Only fire Ended if the gesture actually finished its minimum hold time
            if (performedTriggered) 
            {
                gestureEnded?.Invoke();
                performedTriggered = false;
            }
        }

        wasDetected = detected;

        if (!performedTriggered && detected)
        {
            float holdTimer = Time.time - holdStartTime;
            if (holdTimer >= minimumHoldTime)
            {
                gestureStarted?.Invoke();
                performedTriggered = true;
            }
        }

        lastTimeCheck = Time.time;
    }
}
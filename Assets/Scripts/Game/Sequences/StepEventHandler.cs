using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StepEventHandler : MonoBehaviour
{
    [Tooltip("The ID that matches your SequenceStep ScriptableObject")]
    public string targetStepId; 
    public List<EventsWithDelay> eventsWithDelay;

    public void TriggerEvents()
    {
        if (eventsWithDelay != null)
        {
            foreach (EventsWithDelay eventConfig in eventsWithDelay)
            {
                StartCoroutine(InvokeEventWithDelay(eventConfig.unityEvent, eventConfig.delay));
            }
        }
    }

    IEnumerator InvokeEventWithDelay(UnityEvent unityEvent, float delay)
    {
        yield return new WaitForSeconds(delay);
        unityEvent?.Invoke();
    }
}

[Serializable]
public class EventsWithDelay
{
    public float delay;
    public UnityEvent unityEvent;
}
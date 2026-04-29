using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class SequenceEvent : MonoBehaviour
{
    public List<EventsWithDelay> eventsWithDelay;
    public VATController VATController;
    [Dropdown("GetAnimationIndexes")] public int animationIndex;
    public float simulationDelayStart = 0;

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

    List<int> GetAnimationIndexes()
    {
        List<int> indexes = new List<int>();
        if (VATController == null)
        {
            indexes = new List<int>() {0};
        }
        else
        {
            for (int i = 0; i < VATController.animationData.animations.Count; i++)
            {
                indexes.Add(i);
            }
        }
        return indexes;
    }
}

[Serializable]
public class EventsWithDelay
{
    public float delay;
    public UnityEvent unityEvent;
}
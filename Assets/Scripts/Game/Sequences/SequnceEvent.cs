using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SequenceEvent : MonoBehaviour
{
    public List<EventsWithDelay> eventsWithDelay;
    public VATController VATController;
    [ShowIf("hasController")] [Dropdown("GetAnimationIndexes")] public int animationIndex;
    [ShowIf("hasController")] public float simulationDelayStart = 0;
    [ShowIf("hasController")] public GameTime startTime, endTime;

    bool hasController => VATController != null;
    GameTime currentTime;

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

    public float GetAnimationTime()
    {
        if (VATController == null) return 0;
        VATAnimationData.VATAnimation animation = VATController.animationData.animations[animationIndex];
        float duration = (animation.frameEnd - animation.frameStart + 1) / animation.framerate;
        return duration;
    }
}

[Serializable]
public class EventsWithDelay
{
    public float delay;
    public UnityEvent unityEvent;
}
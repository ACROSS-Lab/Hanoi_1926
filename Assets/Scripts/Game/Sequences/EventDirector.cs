using System.Collections;
using UnityEngine;

public class EventDirector : MonoBehaviour
{
    SequenceEvent[] sequenceEvents;
    SequenceEvent currentSequenceEvent;

    bool simulationFinished = false;

    void Awake()
    {
        sequenceEvents = GetComponentsInChildren<SequenceEvent>();
    }

    public void SetCurrentSequenceEvent(string stepId)
    {
        foreach (SequenceEvent sequenceEvent in sequenceEvents)
        {
            if (string.Equals(sequenceEvent.name, stepId))
            {
                VATController currentController = currentSequenceEvent?.VATController;
                VATController newController = sequenceEvent.VATController;
                
                if (newController != null && newController != currentController)
                {
                    if (currentController != null) currentController.gameObject.SetActive(false);
                    newController.gameObject.SetActive(true);
                }
        
                currentSequenceEvent = sequenceEvent;
                break;
            }
        }
    }

    public void TriggerEventsForStep()
    {
        currentSequenceEvent.TriggerEvents();
    }

    public bool HasSimulationInParallel()
    {
        return currentSequenceEvent.VATController != null;
    }

    public bool IsSimulationFinished()
    {
        return simulationFinished;
    }

    public void PlaySimulation()
    {
        VATController controller = currentSequenceEvent.VATController;
        controller.PlayIndex(currentSequenceEvent.animationIndex);
        StartCoroutine(CountdownAnimtionTime());
    }

    float GetAnimationTime()
    {
        VATController controller = currentSequenceEvent.VATController;
        VATAnimationData.VATAnimation animation = controller.animationData.animations[currentSequenceEvent.animationIndex];
        float duration = (animation.frameEnd - animation.frameStart + 1) / animation.framerate;
        return duration;
    }

    IEnumerator CountdownAnimtionTime()
    {
        simulationFinished = false;
        yield return new WaitForSeconds(GetAnimationTime());
        simulationFinished = true;
    }
}
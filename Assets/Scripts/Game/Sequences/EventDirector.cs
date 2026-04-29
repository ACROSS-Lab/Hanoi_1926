using System.Collections;
using UnityEngine;

public class EventDirector : MonoBehaviour
{
    public SequenceEvent currentSequenceEvent { get; private set; }
    public bool simulationFinished { get; private set; }

    SequenceEvent[] sequenceEvents;
    
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

    public void PlaySimulation()
    {
        StartCoroutine(SimulationCoroutine());
    }

    IEnumerator SimulationCoroutine()
    {
        simulationFinished = false;
        yield return new WaitForSeconds(currentSequenceEvent.simulationDelayStart);
        VATController controller = currentSequenceEvent.VATController;
        controller.PlayIndex(currentSequenceEvent.animationIndex);
        yield return new WaitForSeconds(GetAnimationTime());
        simulationFinished = true;
    }

    float GetAnimationTime()
    {
        VATController controller = currentSequenceEvent.VATController;
        VATAnimationData.VATAnimation animation = controller.animationData.animations[currentSequenceEvent.animationIndex];
        float duration = (animation.frameEnd - animation.frameStart + 1) / animation.framerate;
        return duration;
    }
}
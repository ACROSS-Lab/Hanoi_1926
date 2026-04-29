using UnityEngine;

public class EventDirector : MonoBehaviour
{
    SequenceEvent[] sequenceEvents;
    SequenceEvent currentSequenceEvent;

    void Awake()
    {
        sequenceEvents = GetComponentsInChildren<SequenceEvent>();
    }

    public void SetCurrentSequenceEvent(string stepId)
    {
        SequenceEvent newSquenceEvent;
        foreach (SequenceEvent sequenceEvent in sequenceEvents)
        {
            if (string.Equals(sequenceEvent.name, stepId))
            {
                newSquenceEvent = sequenceEvent;
                
                if (newSquenceEvent.VATController != currentSequenceEvent.VATController)
                {
                    if (currentSequenceEvent.VATController != null) currentSequenceEvent.VATController.gameObject.SetActive(false);
                    if (newSquenceEvent.VATController != null) newSquenceEvent.VATController.gameObject.SetActive(true);
                }
                
                currentSequenceEvent = newSquenceEvent;
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

    public float GetAnimationTime()
    {
        VATController controller = currentSequenceEvent.VATController;
        VATAnimationData.VATAnimation animation = controller.animationData.animations[currentSequenceEvent.animationIndex];
        float duration = (animation.frameEnd - animation.frameStart + 1) / animation.framerate;
        return duration;
    }

    public void PlaySimulation(float presentationDuration)
    {
        VATController controller = currentSequenceEvent.VATController;
        controller.PlayIndex(currentSequenceEvent.animationIndex);
    }
}
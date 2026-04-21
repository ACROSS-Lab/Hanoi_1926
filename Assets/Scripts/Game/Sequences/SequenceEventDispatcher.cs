using UnityEngine;

public class SequenceEventDispatcher : MonoBehaviour
{
    [SerializeField] StepEventHandler[] allStepEvents;

    void Awake()
    {
        allStepEvents = GetComponentsInChildren<StepEventHandler>();
    }

    public void TriggerEventsForStep(string stepId)
    {
        foreach (StepEventHandler handler in allStepEvents)
        {
            if (handler.targetStepId == stepId)
            {
                handler.TriggerEvents();
                break;
            }
        }
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

public class EventDirector : MonoBehaviour
{
    public SequenceEvent currentSequenceEvent { get; private set; }
    public bool simulationFinished { get; private set; }

    [SerializeField] GameTimeManager gameTimeManager;

    SequenceEvent[] sequenceEvents;
    VATController currentController;
    
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

        VATController newController = currentSequenceEvent.VATController;
        bool isSwitching = currentController != newController;
        if (isSwitching)
        {
            newController.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(currentSequenceEvent.simulationDelayStart);
        yield return new WaitUntil(() => newController.hasAnimState);

        newController.PlayIndex(currentSequenceEvent.animationIndex);

        yield return new WaitForSeconds(0.1f);
        if (currentController != null && isSwitching) currentController.gameObject.SetActive(false);
        currentController = newController;

        float duration = currentSequenceEvent.GetAnimationTime();
        gameTimeManager.AdvanceTime(currentSequenceEvent.startTime, currentSequenceEvent.endTime, currentSequenceEvent.startHeight, currentSequenceEvent.endHeight, duration);
        
        yield return new WaitForSeconds(duration);

        simulationFinished = true;
    }
}
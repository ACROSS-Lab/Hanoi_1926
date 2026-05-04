using System.Collections;
using TMPro;
using UnityEngine;

public class EventDirector : MonoBehaviour
{
    public SequenceEvent currentSequenceEvent { get; private set; }
    public bool simulationFinished { get; private set; }

    [SerializeField] TextMeshProUGUI monthText, dayText, hourText;

    SequenceEvent[] sequenceEvents;
    
    void Awake()
    {
        sequenceEvents = GetComponentsInChildren<SequenceEvent>();
    }

    void Start()
    {
        GameTime starDate = new GameTime()
        {
            month = 7,
            day = 23,
            hour = 0
        };
        starDate.UpdateUITimeTexts(monthText, dayText, hourText);
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
        float duration = currentSequenceEvent.GetAnimationTime();

        controller.PlayIndex(currentSequenceEvent.animationIndex);
        currentSequenceEvent.AdvanceTime(monthText, dayText, hourText);
        
        yield return new WaitForSeconds(duration);
        simulationFinished = true;
    }
}
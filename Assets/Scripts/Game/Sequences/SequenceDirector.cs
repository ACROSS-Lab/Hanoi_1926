using System.Collections;
using DG.Tweening;
using UnityEngine;

public class SequenceDirector : MonoBehaviour
{
    [Header("List of steps")]
    [SerializeField] SequenceStep[] sequenceSteps;

    [Header("Player References")]
    [SerializeField] PlayerTransition playerTransition;
 
    [Header("Narrator References")]
    [SerializeField] Narrator narrator;

    [Header("Event Management")]
    [SerializeField] SequenceEventDispatcher eventDispatcher;

    bool hasPerformedAction = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(ExecuteSequence());
    }

    IEnumerator ExecuteSequence()
    {
        foreach (SequenceStep step in sequenceSteps)
        {
            yield return StartCoroutine(ExecuteStep(step));
        }
    }

    IEnumerator ExecuteStep(SequenceStep step)
    {
        if (step.hasNarratorMovement)
        {
            Tween pathTween = narrator.Move(step.targetNarratorPosition, step.offsetAtCenter, step.flyDuration);
            yield return pathTween.WaitForCompletion();
        }

        if (step.hasPlayerMovement)
        {
            playerTransition.MovePlayer(step.playerTargetPosition, step.playerTargetRotation, step.hasSceneTransition, step.sceneName, step.isGoingBackToMainScene);
        }

        if (step.hasDialogue)
        {
            float talkingTime = narrator.UpdateTimeAction(step.dialogueKey, step.bodyState, step.eyesState, step.mouthStartState);
            yield return new WaitForSeconds(talkingTime);
            narrator.FinishDialogue(step.mouthEndState);
            yield return new WaitForSeconds(step.timeWaitAfterTalking);
            narrator.DisableDialogueBox();
        }

        if (eventDispatcher != null)
        {
            eventDispatcher.TriggerEventsForStep(step.name);
        }

        if (step.hasInteraction)
        {
            hasPerformedAction = false;

            float timer = 0;
            bool hasTimeOut = step.waitTimeout > 0;

            while (!hasPerformedAction)
            {
                if (hasTimeOut)
                {
                    timer += Time.deltaTime;
                    if (timer >= step.waitTimeout)
                    {
                        Debug.Log("Time out reached, performing default action");
                        hasPerformedAction = true;
                        break;
                    }
                }
                yield return null;
            }
        }
    }

    public void PerformAction()
    {
        hasPerformedAction = true;
    }
}

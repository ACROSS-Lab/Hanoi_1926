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

    [Header("Debugging")]
    [SerializeField] int debugStepIndex = 0;

    bool hasPerformedAction = false;

    void Start()
    {
        StartCoroutine(ExecuteSequence());
    }

    IEnumerator ExecuteSequence()
    {
        for (int i = 0; i < sequenceSteps.Length; i++)
        {
            SequenceStep step = sequenceSteps[i];
            bool fastForward = i < debugStepIndex;
            yield return StartCoroutine(ExecuteStep(step, fastForward));
        }
    }

    IEnumerator ExecuteStep(SequenceStep step, bool fastForward)
    {
        if (fastForward)
        {
            if (step.hasNarratorMovement)
            {
                narrator.transform.position = step.targetNarratorPosition;
                narrator.transform.localScale = new Vector3(step.targetNarratorScale, step.targetNarratorScale, step.targetNarratorScale);  
            }

            if (step.hasPlayerMovement)
            {
                playerTransition.transform.position = step.playerTargetPosition;
                playerTransition.transform.rotation = Quaternion.Euler(step.playerTargetRotation);
            }

            if (eventDispatcher != null)
            {
                eventDispatcher.TriggerEventsForStep(step.name);
            }

            yield break;
        }

        if (step.hasPlayerMovement)
        {
            playerTransition.MovePlayer(step.playerTargetPosition, step.hasPlayerRotation, step.playerTargetRotation, step.hasSceneTransition, step.sceneName, step.isGoingBackToMainScene);
        }

        if (step.hasNarratorMovement)
        {
            Tween action = narrator.Move(step.targetNarratorPosition, step.offsetAtCenter, step.targetNarratorScale, step.flyDuration);
            yield return action.WaitForCompletion();
        }

        if (eventDispatcher != null)
        {
            eventDispatcher.TriggerEventsForStep(step.name);
        }

        if (step.hasDialogue)
        {
            yield return new WaitForSeconds(step.timeWaitBeforeTalking);
            float talkingTime = narrator.StartTalking(step.dialogueKey, step.bodyState, step.eyesState, step.mouthStartState, step.isUsingOverlay);
            yield return new WaitForSeconds(talkingTime);
            narrator.FinishDialogue(step.mouthEndState);
            yield return new WaitForSeconds(step.timeWaitAfterTalking);
            narrator.DisableDialogueBox();
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

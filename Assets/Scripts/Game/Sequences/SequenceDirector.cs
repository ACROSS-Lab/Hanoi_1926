using System.Collections;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class SequenceDirector : MonoBehaviour
{
    [Header("List of steps")]
    [SerializeField] SequenceStep[] sequenceSteps;

    [Header("Player References")]
    [SerializeField] PlayerTransition playerTransition;
    [SerializeField] PlayerCameraController playerCameraController;
 
    [Header("Narrator References")]
    [SerializeField] Narrator narrator;

    [Header("Event Management")]
    [SerializeField] EventDirector eventDirector;

    [Header("Debugging")]
    [SerializeField] int debugStepIndex = 0;
    [ShowNonSerializedField] int currentStepIndex = 0;

    bool hasPerformedAction = false;

    void Awake()
    {
        if (playerCameraController == null)
        {
            playerCameraController = FindFirstObjectByType<PlayerCameraController>();
        }
        if (playerTransition == null && playerCameraController == null)
        {
            playerTransition = FindFirstObjectByType<PlayerTransition>();
        }
    }

    void Start()
    {
        StartCoroutine(ExecuteSequence());
    }

    IEnumerator ExecuteSequence()
    {
        for (int i = 0; i < sequenceSteps.Length; i++)
        {
            SequenceStep step = sequenceSteps[i];
            currentStepIndex = i;
            bool fastForward = false;
            #if UNITY_EDITOR
            fastForward = i < debugStepIndex;
            #endif
            yield return StartCoroutine(ExecuteStep(step, fastForward));
        }
    }

    IEnumerator ExecuteStep(SequenceStep step, bool fastForward)
    {
        #if UNITY_EDITOR
        if (fastForward)
        {
            ApplyFastForward(step);
            yield break;
        }
        #endif

        yield return HandleEventsStart(step);
        HandlePlayer(step);
        yield return HandleNarrator(step);
        yield return HandleDialogue(step);
        yield return HandleInteraction(step);
        yield return HandleParallelSimulation(step);
    }

    void ApplyFastForward(SequenceStep step)
    {
        if (step.hasSequenceEvents)
        {
            eventDirector.SetCurrentSequenceEvent(step.name);
            eventDirector.TriggerEventsForStep();
        }

        if (step.hasNarratorMovement)
        {
            narrator.transform.position = step.targetNarratorPosition;
            narrator.transform.localScale = Vector3.one * step.targetNarratorScale;
        }

        if (step.hasPlayerMovement)
        {
            if (playerCameraController != null)
            {
                playerCameraController.SetPositionAndRotation(
                    step.playerTargetPosition,
                    step.hasPlayerRotation ? Quaternion.Euler(step.playerTargetRotation) : playerCameraController.transform.rotation
                );
            }
            else if (playerTransition != null)
            {
                playerTransition.transform.SetPositionAndRotation(
                    step.playerTargetPosition,
                    Quaternion.Euler(step.playerTargetRotation)
                );
            }
        }
    }

    IEnumerator HandleEventsStart(SequenceStep step)
    {
        if (!step.hasSequenceEvents) yield break;

        eventDirector.SetCurrentSequenceEvent(step.name);
        eventDirector.TriggerEventsForStep();

        if (eventDirector.HasSimulationInParallel())
        {
            eventDirector.PlaySimulation();
        }

        yield break;
    }

    void HandlePlayer(SequenceStep step)
    {
        if (!step.hasPlayerMovement) return;

        if (playerCameraController != null)
        {
            playerCameraController.MovePlayer(
                step.playerTargetPosition,
                step.hasPlayerRotation,
                step.playerTargetRotation,
                step.hasSceneTransition,
                step.sceneName,
                step.isGoingBackToMainScene
            );
        }
        else if (playerTransition != null)
        {
            playerTransition.MovePlayer(
                step.playerTargetPosition,
                step.hasPlayerRotation,
                step.playerTargetRotation,
                step.hasSceneTransition,
                step.sceneName,
                step.isGoingBackToMainScene
            );
        }
    }

    IEnumerator HandleNarrator(SequenceStep step)
    {
        if (!step.hasNarratorMovement) yield break;

        Tween action = narrator.Move(
            step.targetNarratorPosition,
            step.offsetAtCenter,
            step.hasNarratorRotation,
            step.targetNarratorRotation,
            step.targetNarratorScale,
            step.flyDuration
        );

        yield return action.WaitForCompletion();
    }

    IEnumerator HandleDialogue(SequenceStep step)
    {
        if (!step.hasDialogue) yield break;

        yield return new WaitForSeconds(step.timeWaitBeforeTalking);

        float talkingTime = narrator.StartTalking(
            step.dialogueKey,
            step.bodyStartState,
            step.eyesState,
            step.mouthStartState,
            step.isUsingOverlay
        );

        yield return new WaitForSeconds(talkingTime);

        narrator.FinishDialogue(step.bodyEndState, step.mouthEndState);

        yield return new WaitForSeconds(step.timeWaitAfterTalking);

        narrator.DisableDialogueBox();
    }

    IEnumerator HandleInteraction(SequenceStep step)
    {
        if (!step.hasInteraction) yield break;

        hasPerformedAction = false;

        float timer = 0f;

        while (!hasPerformedAction)
        {
            timer += Time.deltaTime;
            if (timer >= step.waitTimeout)
            {
                Debug.Log("Timeout reached, performing default action");
                break;
            }

            yield return null;
        }

        hasPerformedAction = true;
    }

    IEnumerator HandleParallelSimulation(SequenceStep step)
    {
        if (!step.hasSequenceEvents) yield break;
        if (!eventDirector.HasSimulationInParallel()) yield break;

        while (!eventDirector.simulationFinished)
        {
            yield return null;
        }
    }

    public void PerformAction()
    {
        hasPerformedAction = true;
        Debug.Log("Performed Action");
    }
}

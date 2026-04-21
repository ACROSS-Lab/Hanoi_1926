using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class SequenceDirector : MonoBehaviour
{
    [Header("List of steps")]
    [SerializeField] SequenceStep[] sequenceSteps;

    [Header("Player References")]
    [SerializeField] Transform playerTransform;
    [SerializeField] VignetteTrigger vignetteTrigger;
 
    [Header("Narrator References")]
    [SerializeField] LocalizedKey narratorLocalization;
    [SerializeField] AudioSource narratorAudioSource;
    [SerializeField] Animator narratorAnimator;

    [Header("Event Management")]
    [SerializeField] SequenceEventDispatcher eventDispatcher;

    bool hasPerformedAction = false;

    void Start()
    {
        
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
        if (step.hasMovement)
        {
            vignetteTrigger.StartVignette();
            Vector3 midPoint = Vector3.Lerp(playerTransform.position, step.targetPosition, 0.5f) + step.offsetAtCenter;
            Vector3[] path = {playerTransform.position, midPoint, step.targetPosition};
            Tween pathTween = playerTransform.DOPath(path, step.flyDuration, PathType.CatmullRom).SetEase(Ease.InOutSine);
            yield return pathTween.WaitForCompletion();
            vignetteTrigger.StopVignette();
        }

        if (step.hasDialogue)
        {
            narratorLocalization.localizationKey = step.dialogueKey;
            narratorLocalization.UpdateText();
            narratorLocalization.UpdateAudioClip();

            narratorAnimator.SetFloat("BodyState", step.bodyState);
            narratorAnimator.SetFloat("EyesState", step.eyesState);
            narratorAnimator.SetFloat("MouthState", step.mouthState);

            narratorAudioSource.Play();
            float talkingTime = narratorAudioSource.clip.length;
            yield return new WaitForSeconds(talkingTime + step.timeWaitAfterTalking);
        }

        if (step.hasInteraction)
        {
            if (eventDispatcher != null)
            {
                eventDispatcher.TriggerEventsForStep(step.stepId);
            }

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

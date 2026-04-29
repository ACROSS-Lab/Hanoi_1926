using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Sequence Step", menuName = "Sequence Step")]
public class SequenceStep : ScriptableObject
{
    public bool hasSequenceEvents;

    [Header("Phase 1: Movement")]
    public bool hasNarratorMovement;
    [ShowIf("hasNarratorMovement")] public Vector3 targetNarratorPosition;
    [ShowIf("hasNarratorMovement")] public Vector3 offsetAtCenter;
    [ShowIf("hasNarratorMovement")] public bool hasNarratorRotation;
    [ShowIf("hasNarratorRotation")] public Vector3 targetNarratorRotation;
    [ShowIf("hasNarratorMovement")] public float targetNarratorScale;
    [ShowIf("hasNarratorMovement")] public float flyDuration;

    public bool hasPlayerMovement;
    [ShowIf("hasPlayerMovement")] public Vector3 playerTargetPosition;
    [ShowIf("hasPlayerMovement")] public bool hasPlayerRotation;
    [ShowIf("hasPlayerRotation")] public Vector3 playerTargetRotation;
    [ShowIf("hasPlayerMovement")] public bool hasSceneTransition;
    [ShowIf("hasSceneTransition")] public string sceneName;
    [ShowIf("hasSceneTransition")] public bool isGoingBackToMainScene;

    [Header("Phase 2: Presentation")]
    public bool hasDialogue;
    [ShowIf("hasDialogue")] public float timeWaitBeforeTalking;
    [ShowIf("hasDialogue")] public string dialogueKey;
    [ShowIf("hasDialogue")] public bool isUsingOverlay;
    [ShowIf("hasDialogue")] public int bodyState, eyesState;
    [ShowIf("hasDialogue")] public int mouthStartState, mouthEndState;
    [ShowIf("hasDialogue")] public float timeWaitAfterTalking;

    [Header("Phase 3: Interaction")]
    public bool hasInteraction;
    [ShowIf("hasInteraction")] public float waitTimeout;
}
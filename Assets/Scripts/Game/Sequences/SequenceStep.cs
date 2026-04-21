using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "Sequence Step", menuName = "Sequence Step")]
public class SequenceStep : ScriptableObject
{
    [Header("Identification")]
    public string stepId;

    [Header("Phase 1: Movement")]
    public bool hasMovement;
    [ShowIf("hasMovement")] public Vector3 targetPosition;
    [ShowIf("hasMovement")] public Vector3 offsetAtCenter;
    [ShowIf("hasMovement")] public float flyDuration;

    [Header("Phase 2: Presentation")]
    public bool hasDialogue;
    [ShowIf("hasDialogue")] public string dialogueKey;
    [ShowIf("hasDialogue")] public int bodyState, eyesState, mouthState;
    [ShowIf("hasDialogue")] public float timeWaitAfterTalking;

    [Header("Phase 3: Interaction")]
    public bool hasInteraction;
    [ShowIf("hasInteraction")] public PlayerActionType waitForAction;
    [ShowIf("hasInteraction")] public float waitTimeout;
}
public enum PlayerActionType 
{ 
    NONE,
    CLICK,
    WATCH,
}
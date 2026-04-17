using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VATSequencer : MonoBehaviour
{
    [System.Serializable]
    public struct VATStep
    {
        public VATController vat;
        public int animIndex;
        public float transition;
    }

    [Header("Sequence")]
    public List<VATStep> steps;

    [Header("Events")]
    public UnityEvent onSequenceStart;
    public UnityEvent onSequenceEnd;
    public UnityEvent<int, VATStep> onStepChanged;

    // Callbacks C# — pour le script externe
    public event Action OnSequenceStart;
    public event Action OnSequenceEnd;
    public event Action<int, VATStep> OnStepChanged;

    public int CurrentStepIndex { get; private set; } = -1;
    public bool IsStarted => CurrentStepIndex >= 0;
    public bool IsFinished => CurrentStepIndex >= steps.Count;

    public VATStep? CurrentStep =>
        CurrentStepIndex >= 0 && CurrentStepIndex < steps.Count
        ? steps[CurrentStepIndex]
        : null;

    // =============================================
    //  PUBLIC API
    // =============================================

    /// <summary>
    /// Démarre la séquence sur le premier step.
    /// Appelé par le script externe pour initialiser.
    /// </summary>
    public void StartSequence()
    {
        if (steps == null || steps.Count == 0) return;

        // Tout désactiver au départ
        foreach (var s in steps)
            if (s.vat != null) s.vat.gameObject.SetActive(false);

        CurrentStepIndex = -1;

        onSequenceStart?.Invoke();
        OnSequenceStart?.Invoke();

        NextStep();
    }

    /// <summary>
    /// Avance au step suivant.
    /// Appelé par le script externe à chaque progression du jeu.
    /// </summary>
    public void NextStep()
    {
        int next = CurrentStepIndex + 1;

        if (next >= steps.Count)
        {
            onSequenceEnd?.Invoke();
            OnSequenceEnd?.Invoke();
            return;
        }

        ActivateStep(next);
    }

    /// <summary>
    /// Saute directement à un step précis (pour le script externe modulaire).
    /// </summary>
    public void GoToStep(int index)
    {
        if (index < 0 || index >= steps.Count) return;
        ActivateStep(index);
    }

    // =============================================
    //  INTERNAL
    // =============================================

    private void ActivateStep(int index)
    {
        // Désactiver le step courant
        if (CurrentStepIndex >= 0 && CurrentStepIndex < steps.Count)
        {
            var prev = steps[CurrentStepIndex];
            if (prev.vat != null)
                prev.vat.gameObject.SetActive(false);
        }

        CurrentStepIndex = index;
        var step = steps[index];

        if (step.vat != null)
        {
            step.vat.gameObject.SetActive(true);
            step.vat.PlayIndex(step.animIndex, step.transition);
        }

        onStepChanged?.Invoke(index, step);
        OnStepChanged?.Invoke(index, step);
    }
}
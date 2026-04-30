// VATController.cs
// Author: Luke Stilson
// Modified: Added Pause / Resume / Stop public API

using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("OpenVAT/VAT Controller")]
[RequireComponent(typeof(Renderer))]
public class VATController : MonoBehaviour
{
    [Header("Playback Mode")]
    private bool useGpuTimeline = true;

    public float Speed
    {
        get => animState.speedMultiplier;
        set => animState.speedMultiplier = Mathf.Max(0f, value);
    }

    public enum PlayInitMode { Single, Sequence }

    [Header("Animation Data")]
    public VATAnimationData animationData;

    [Header("Init Playback")]
    public PlayInitMode playInitMode    = PlayInitMode.Single;
    public int          singleAnimIndex = 0;
    public int          seqStartIndex   = 0;
    public int          seqEndIndex     = 0;
    public float        seqTransition   = 0.25f;
    public bool         seqLoop         = false;

    private VATAnimStateMachine animState;
    private VATMaterialBinder   binder;
    private VATMaterialState    _lastState;

    /// <summary>Expose the animation list in the editor.</summary>
    public List<VATAnimationData.VATAnimation> Anims
        => animationData ? animationData.animations : null;

    /// <summary>True while the animation is paused.</summary>
    public bool IsPaused => animState != null && animState.IsPaused;

    // =============================================
    //  UNITY LIFECYCLE
    // =============================================

    void Awake()
    {
        animState = new VATAnimStateMachine();
        if (binder == null)
            binder = new VATMaterialBinder(GetComponent<Renderer>());
        InitPlayback();
    }

    void Update()
    {
        // Always call ApplyState every frame.
        // When paused, VATAnimStateMachine slides the time offsets internally
        // so the shader stays frozen on the correct frame.
        ApplyState();
    }

    // =============================================
    //  INIT
    // =============================================

    private void InitPlayback()
    {
        _lastState = default;
        var anims = Anims;
        if (animationData == null || anims == null || anims.Count == 0)
            return;

        int start = Mathf.Clamp(singleAnimIndex, 0, anims.Count - 1);
        animState.Initialize(anims, start);

        switch (playInitMode)
        {
            case PlayInitMode.Single:
                break;

            case PlayInitMode.Sequence:
                seqStartIndex = Mathf.Clamp(seqStartIndex, 0, anims.Count - 1);
                seqEndIndex   = Mathf.Clamp(seqEndIndex,   0, anims.Count - 1);
                animState.PlaySequence(seqStartIndex, seqEndIndex, seqTransition, seqLoop, 0f);
                break;
        }

        ApplyState(force: true);
    }

    // =============================================
    //  APPLY STATE
    // =============================================

    private void ApplyState(bool force = false)
    {
        var state = animState.UpdateAndGetState(Time.deltaTime);
        state.useGpuTimeline = useGpuTimeline;

        if (force || !_lastState.Equals(state))
        {
            binder.ApplyState(state);
            _lastState = state;
        }
    }

    // =============================================
    //  PUBLIC PLAYBACK API
    // =============================================

    public void PlayIndex(int index, float transitionTime = 0.25f)
    {
        animState.PlayIndex(index, transitionTime);
        ApplyState(force: false);
    }

    public void Play(string name, float transitionTime = 0.25f)
    {
        animState.Play(name, transitionTime);
        ApplyState(force: false);
    }

    public void PlayInstant(int index)
    {
        animState.PlayIndex(index, 0f); // corrected: 0f for instant
        ApplyState(force: false);
    }

    public void PlaySequence(int minIndex, int maxIndex, float transitionTime = 0.25f)
    {
        animState.PlaySequence(minIndex, maxIndex, transitionTime);
        ApplyState(force: false);
    }

    // =============================================
    //  PAUSE / RESUME / STOP
    // =============================================

    /// <summary>Freeze the animation on its current frame.</summary>
    public void Pause()
    {
        animState.Pause();
    }

    /// <summary>Resume from the exact frame it was paused on.</summary>
    public void Resume()
    {
        animState.Resume();
        ApplyState(force: true);
    }

    /// <summary>
    /// Stop and rewind to the beginning of <paramref name="index"/>
    /// (defaults to the inspector's singleAnimIndex).
    /// </summary>
    public void Stop(int index = -1)
    {
        int resetTo = index >= 0 ? index : singleAnimIndex;
        animState.Stop(resetTo);
        ApplyState(force: true);
    }

    /// <summary>Toggle between Pause and Resume.</summary>
    public void TogglePause()
    {
        if (IsPaused) Resume();
        else          Pause();
    }
}

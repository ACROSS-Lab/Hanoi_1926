// VATAnimStateMachine.cs
// Author: Luke Stilson
// Modified: Added Pause / Resume / Stop support
//
// HOW THE PAUSE WORKS:
// The shader computes:  frame_time = _Time.y - _TimeOffsetA
// _Time.y is Unity's built-in GPU clock — it can't be stopped.
// So instead, during a pause we continuously slide _TimeOffsetA forward
// at the same rate as _Time.y, keeping (Time.time - timeOffsetA) frozen.
// On Resume(), the offsets are already correct — we just stop sliding them.

using System.Collections.Generic;
using UnityEngine;

public class VATAnimStateMachine
{
    // Animation data
    private List<VATAnimationData.VATAnimation> anims;
    private int animAIndex = -1, animBIndex = -1;
    private bool usingA = true;

    // Per-track frame state (CPU-visible but not necessarily used by GPU path)
    private int frameIndexA, frameIndexB;
    private float interpA, interpB;
    private int nextFrameA, nextFrameB;

    // Shader/GPU timing & blend bookkeeping
    private float blendTimer, blendDuration;
    private bool blendForward = true;
    private float blendStartTimeAbs = 0f;
    private bool isInitialized = false;
    private float timeOffsetA = 0f;
    private float timeOffsetB = 0f;

    // Sequence outputs for the shader
    private float seqStartA = -1f, seqStartB = -1f;
    private float useSeqA = 0f, useSeqB = 0f;

    private VATMaterialState lastMaterialState;

    // =============================================
    //  PAUSE / RESUME / STOP STATE
    // =============================================

    private bool _isPaused = false;

    // Frozen relative times: (Time.time - offset) captured at pause moment
    private float _frozenRelativeA   = 0f;
    private float _frozenRelativeB   = 0f;
    private float _frozenRelativeBlend = 0f;

    /// <summary>True while the animation is paused.</summary>
    public bool IsPaused => _isPaused;

    /// <summary>1 = vitesse normale, 0.5 = moitié, 2 = double.</summary>
    public float speedMultiplier = 1f;

    /// <summary>
    /// Freeze the animation on its current frame.
    /// Safe to call multiple times — second call is a no-op.
    /// </summary>
    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        // Capture the current relative times so we can hold them constant.
        float now = Time.time;
        _frozenRelativeA     = now - timeOffsetA;
        _frozenRelativeB     = now - timeOffsetB;
        _frozenRelativeBlend = now - blendStartTimeAbs;
    }

    /// <summary>
    /// Resume playback from the exact frame it was paused on.
    /// Safe to call when not paused — no-op.
    /// </summary>
    public void Resume()
    {
        if (!_isPaused) return;

        // Slide offsets so that (Time.time - offset) still equals the frozen value.
        // From this point UpdateAndGetState will stop adjusting them.
        float now = Time.time;
        timeOffsetA       = now - _frozenRelativeA;
        timeOffsetB       = now - _frozenRelativeB;
        blendStartTimeAbs = now - _frozenRelativeBlend;

        _isPaused = false;
    }

    /// <summary>
    /// Stop and rewind to the beginning of <paramref name="resetIndex"/>.
    /// Clears any active blend. Leaves the machine in a non-paused state.
    /// </summary>
    public void Stop(int resetIndex)
    {
        // Clear pause state so Initialize() can run cleanly.
        _isPaused = false;

        // Re-initialize resets timers and sets the clip from scratch.
        isInitialized = false;
        if (anims != null && anims.Count > 0)
            Initialize(anims, resetIndex);
    }

    // =============================================
    //  INIT / CORE
    // =============================================

    public void Initialize(List<VATAnimationData.VATAnimation> anims, int startIndex)
    {
        if (isInitialized) return;
        this.anims = anims;
        if (anims == null || anims.Count == 0) return;

        animAIndex = Mathf.Clamp(startIndex, 0, anims.Count - 1);
        SetTrackState(toA: true, index: animAIndex, startFrame: anims[animAIndex].frameStart);

        animBIndex = animAIndex;
        SetTrackState(toA: false, index: animBIndex, startFrame: anims[animBIndex].frameStart);

        useSeqA = 0f; useSeqB = 0f;
        seqStartA = -1f; seqStartB = -1f;

        blendDuration = 0f;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
        usingA = true;
        blendForward = true;
        isInitialized = true;
    }

    // =============================================
    //  PLAY METHODS
    // =============================================

    public void PlayIndex(int index, float transitionTime = 0.25f)
    {
        if (anims == null || index < 0 || index >= anims.Count) return;
        if (!isInitialized) { Initialize(anims, index); return; }

        // Resume automatically if paused so offsets are correct before we mutate them.
        if (_isPaused) Resume();

        if (blendDuration > 0f && blendTimer >= blendDuration)
        {
            blendDuration = 0f;
            blendTimer = 0f;
        }

        usingA = !usingA;

        if (transitionTime <= 0f)
        {
            if (usingA) SetTrackState(true, index, anims[index].frameStart);
            else        SetTrackState(false, index, anims[index].frameStart);

            blendDuration = 0f;
            blendTimer = 0f;
            blendStartTimeAbs = Time.time;
            return;
        }

        if (usingA)
        {
            animBIndex = index;
            SetTrackState(false, animBIndex, anims[animBIndex].frameStart);
            blendForward = true;
        }
        else
        {
            animAIndex = index;
            SetTrackState(true, animAIndex, anims[animAIndex].frameStart);
            blendForward = false;
        }

        blendDuration = transitionTime;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
    }

    public void Play(string name, float transitionTime = 0.25f)
    {
        int index = anims?.FindIndex(a => a.name == name) ?? -1;
        if (index < 0) return;
        PlayIndex(index, transitionTime);
    }

    public void PlayIndexRandomStart(int index, float transitionTime = 0.25f)
    {
        if (anims == null || index < 0 || index >= anims.Count) return;

        if (_isPaused) Resume();

        var anim = anims[index];
        int randomFrame = Random.Range(anim.frameStart, anim.frameEnd + 1);

        if (!isInitialized)
        {
            Initialize(anims, index);
            SetTrackState(true, index, randomFrame);
            SetTrackState(false, index, randomFrame);
            return;
        }

        usingA = !usingA;

        if (transitionTime <= 0f)
        {
            if (usingA) SetTrackState(true, index, randomFrame);
            else        SetTrackState(false, index, randomFrame);

            blendDuration = 0f;
            blendTimer = 0f;
            blendStartTimeAbs = Time.time;
            return;
        }

        if (usingA)
        {
            animBIndex = index;
            SetTrackState(false, animBIndex, randomFrame);
            blendForward = true;
        }
        else
        {
            animAIndex = index;
            SetTrackState(true, animAIndex, randomFrame);
            blendForward = false;
        }

        blendDuration = transitionTime;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
    }

    public void PlaySequence(int minIndex, int maxIndex, float stepTransition = 0.25f, bool _loopIgnored = false, float initialTransition = 0f)
    {
        if (anims == null || minIndex < 0 || maxIndex >= anims.Count || minIndex > maxIndex) return;

        if (_isPaused) Resume();

        int firstIndex = minIndex;
        int lastIndex  = maxIndex;

        int firstStartFrame = anims[firstIndex].frameStart;
        int lastStartFrame  = anims[lastIndex].frameStart;

        usingA = !usingA;

        if (initialTransition <= 0f)
        {
            if (usingA) SetSequenceOnTrackA(lastIndex, firstStartFrame, lastStartFrame);
            else        SetSequenceOnTrackB(lastIndex, firstStartFrame, lastStartFrame);

            blendDuration = 0f;
            blendTimer = 0f;
            blendStartTimeAbs = Time.time;
            return;
        }

        if (usingA)
        {
            animBIndex = lastIndex;
            SetSequenceOnTrackB(lastIndex, firstStartFrame, lastStartFrame);
            blendForward = true;
        }
        else
        {
            animAIndex = lastIndex;
            SetSequenceOnTrackA(lastIndex, firstStartFrame, lastStartFrame);
            blendForward = false;
        }

        blendDuration = stepTransition;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
    }

    public void PlayNext(float transitionTime = 0.25f)
    {
        if (anims == null || anims.Count == 0) return;
        int current = usingA ? animAIndex : animBIndex;
        int next = (current + 1) % anims.Count;
        PlayIndex(next, transitionTime);
    }

    // =============================================
    //  UPDATE
    // =============================================

    public VATMaterialState UpdateAndGetState(float deltaTime)
    {
        // ── PAUSE TRICK ──────────────────────────────────────────────────────
        // While paused, slide the offsets forward at the same rate as Time.time
        // so that (Time.time - offset) stays constant → shader sees frozen frame.
        if (_isPaused)
        {
            float now = Time.time;
            timeOffsetA       = now - _frozenRelativeA;
            timeOffsetB       = now - _frozenRelativeB;
            blendStartTimeAbs = now - _frozenRelativeBlend;
        }
        // ─────────────────────────────────────────────────────────────────────


        var animA = anims[animAIndex];
        var animB = (animBIndex >= 0) ? anims[animBIndex] : animA;

        lastMaterialState = new VATMaterialState
        {
            frameIndexA = frameIndexA,
            nextFrameA  = nextFrameA,
            interpA     = interpA,
            frameIndexB = frameIndexB,
            nextFrameB  = nextFrameB,
            interpB     = interpB,

            frameStartA = animA.frameStart,
            frameEndA   = animA.frameEnd,
            frameStartB = animB.frameStart,
            frameEndB   = animB.frameEnd,


            fpsA = animA.framerate * speedMultiplier,
            fpsB = animB.framerate * speedMultiplier,
            loopA = animA.looping ? 1f : 0f,
            loopB = animB.looping ? 1f : 0f,

            blendStartTime = blendStartTimeAbs,
            blendDuration  = blendDuration,
            blendDirection = blendForward ? 1f : 0f,

            timeOffsetA = timeOffsetA,
            timeOffsetB = timeOffsetB,

            seqStartA = seqStartA,
            seqStartB = seqStartB,
            useSeqA   = useSeqA,
            useSeqB   = useSeqB,
        };

        return lastMaterialState;
    }

    // =============================================
    //  HELPERS
    // =============================================

    private void SetTrackState(bool toA, int index, int startFrame)
    {
        if (toA)
        {
            animAIndex  = index;
            frameIndexA = startFrame;
            interpA     = 0f;
            nextFrameA  = frameIndexA + 1;
            timeOffsetA = Time.time;
            useSeqA     = 0f;
            seqStartA   = -1f;
        }
        else
        {
            animBIndex  = index;
            frameIndexB = startFrame;
            interpB     = 0f;
            nextFrameB  = frameIndexB + 1;
            timeOffsetB = Time.time;
            useSeqB     = 0f;
            seqStartB   = -1f;
        }
    }

    private void SetSequenceOnTrackA(int lastIndex, int seqStartFrame, int lastStartFrame)
    {
        animAIndex  = lastIndex;
        frameIndexA = lastStartFrame;
        interpA     = 0f;
        nextFrameA  = frameIndexA + 1;
        timeOffsetA = Time.time;
        seqStartA   = seqStartFrame;
        useSeqA     = 1f;
        useSeqB     = 0f;
    }

    private void SetSequenceOnTrackB(int lastIndex, int seqStartFrame, int lastStartFrame)
    {
        animBIndex  = lastIndex;
        frameIndexB = lastStartFrame;
        interpB     = 0f;
        nextFrameB  = frameIndexB + 1;
        timeOffsetB = Time.time;
        seqStartB   = seqStartFrame;
        useSeqB     = 1f;
        useSeqA     = 0f;
    }
}

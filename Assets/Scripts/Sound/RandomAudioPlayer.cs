using UnityEngine;

/// <summary>
/// RandomAudioPlayer — Attach this script to any GameObject with an AudioSource
/// to get a random start offset and/or random pitch on every play.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RandomAudioPlayer : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  RANDOM OFFSET
    // ─────────────────────────────────────────────

    [Header("Random Offset")]
    [Tooltip("Start playback at a random position within the clip.")]
    public bool useRandomOffset = false;

    [Range(0f, 1f)]
    [Tooltip("Minimum position in the clip (0 = start, 1 = end).")]
    public float offsetMin = 0f;

    [Range(0f, 1f)]
    [Tooltip("Maximum position in the clip (0 = start, 1 = end).")]
    public float offsetMax = 1f;

    // ─────────────────────────────────────────────
    //  RANDOM PITCH
    // ─────────────────────────────────────────────

    [Header("Random Pitch")]
    [Tooltip("Apply a random pitch variation on every play.")]
    public bool useRandomPitch = false;

    [Range(0.1f, 3f)]
    [Tooltip("Minimum pitch (1 = normal, <1 = lower, >1 = higher).")]
    public float pitchMin = 0.9f;

    [Range(0.1f, 3f)]
    [Tooltip("Maximum pitch.")]
    public float pitchMax = 1.1f;

    // ─────────────────────────────────────────────
    //  RANDOM VOLUME (bonus)
    // ─────────────────────────────────────────────

    [Header("Random Volume (optional)")]
    [Tooltip("Apply a random volume variation on every play.")]
    public bool useRandomVolume = false;

    [Range(0f, 1f)]
    public float volumeMin = 0.8f;

    [Range(0f, 1f)]
    public float volumeMax = 1f;

    // ─────────────────────────────────────────────
    //  PLAYBACK OPTIONS
    // ─────────────────────────────────────────────

    [Header("Playback Options")]
    [Tooltip("Apply randomization and play automatically on start.")]
    public bool playOnAwake = true;

    // ─────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────

    private AudioSource _source;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _source = GetComponent<AudioSource>();

        // Disable the native playOnAwake so we control playback timing
        _source.playOnAwake = false;

        if (playOnAwake)
            Play();
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Applies all enabled randomizations, then starts playback.
    /// Call this instead of AudioSource.Play().
    /// </summary>
    public void Play()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source.clip == null)
        {
            Debug.LogWarning($"[RandomAudioPlayer] No clip assigned on {gameObject.name}.", this);
            return;
        }

        ApplyRandomPitch();
        ApplyRandomVolume();

        if (useRandomOffset)
            PlayWithRandomOffset();
        else
            _source.Play();
    }

    /// <summary>
    /// Applies randomization without triggering playback.
    /// Useful if you call AudioSource.Play() yourself afterward.
    /// </summary>
    public void ApplyRandomization()
    {
        ApplyRandomPitch();
        ApplyRandomVolume();

        if (useRandomOffset && _source.clip != null)
        {
            float t = Random.Range(
                Mathf.Clamp01(offsetMin),
                Mathf.Clamp01(offsetMax)
            );
            _source.time = t * _source.clip.length;
        }
    }

    // ─────────────────────────────────────────────
    //  PRIVATE — helpers
    // ─────────────────────────────────────────────

    private void ApplyRandomPitch()
    {
        if (!useRandomPitch) return;

        float min = Mathf.Min(pitchMin, pitchMax);
        float max = Mathf.Max(pitchMin, pitchMax);
        _source.pitch = Random.Range(min, max);
    }

    private void ApplyRandomVolume()
    {
        if (!useRandomVolume) return;

        float min = Mathf.Min(volumeMin, volumeMax);
        float max = Mathf.Max(volumeMin, volumeMax);
        _source.volume = Random.Range(min, max);
    }

    private void PlayWithRandomOffset()
    {
        float min = Mathf.Clamp01(Mathf.Min(offsetMin, offsetMax));
        float max = Mathf.Clamp01(Mathf.Max(offsetMin, offsetMax));
        float normalizedTime = Random.Range(min, max);

        // timeSamples is more accurate than .time for compressed clips
        int totalSamples = _source.clip.samples;
        _source.timeSamples = Mathf.FloorToInt(normalizedTime * totalSamples);
        _source.Play();
    }

    // ─────────────────────────────────────────────
    //  EDITOR VALIDATION
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure min <= max for all ranges
        if (offsetMin > offsetMax) offsetMax = offsetMin;
        if (pitchMin  > pitchMax)  pitchMax  = pitchMin;
        if (volumeMin > volumeMax) volumeMax = volumeMin;
    }
#endif
}

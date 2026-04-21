using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

public class VignetteTrigger : MonoBehaviour, ITunnelingVignetteProvider
{
    [SerializeField] private TunnelingVignetteController vignetteController;

    // This property is required by the ITunnelingVignetteProvider interface.
    // Initializing it prevents null errors. You can even modify this in code 
    // to dynamically change the vignette size/color based on the specific sequence step!
    public VignetteParameters vignetteParameters { get; set; } = new VignetteParameters();

    public void StartVignette()
    {
        if (vignetteController != null)
        {
            vignetteController.BeginTunnelingVignette(this);
        }
    }

    public void StopVignette()
    {
        if (vignetteController != null)
        {
            vignetteController.EndTunnelingVignette(this);
        }
    }
}
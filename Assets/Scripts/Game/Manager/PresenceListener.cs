using UnityEngine;
using UnityEngine.XR.OpenXR;

public class PresenceListener : MonoBehaviour
{
    [SerializeField] PlayerTransition playerTransition;
    [SerializeField] float timeToReturnToMainScene = 5f;
    [SerializeField] string menuSceneName;

    bool wasUserPresent = true;
    float timeHeadsetRemoved;

    void Update()
    {
        bool isUserPresent = OpenXRUtility.IsSessionFocused; 

        if (isUserPresent != wasUserPresent)
        {
            if (isUserPresent)
            {
                Time.timeScale = 1f;
                Debug.Log("User is active and wearing the headset.");
                if (timeHeadsetRemoved + timeToReturnToMainScene < Time.unscaledTime)
                {
                    playerTransition.LoadNormalSceneFadeOut(menuSceneName);
                    Debug.Log("Returning to main scene.");
                }
            }
            else
            {
                Debug.Log("User has removed the headset or opened system menu.");
                timeHeadsetRemoved = Time.unscaledTime;
                Time.timeScale = 0f;
            }

            wasUserPresent = isUserPresent;
        }
    }
}
using UnityEngine;

public class AndroidListener : MonoBehaviour
{

    [SerializeField] PlayerTransition playerTransition;
    [SerializeField] float timeToReturnToMainScene = 5f;
    [SerializeField] string menuSceneName;

    AndroidJavaObject audioManager;
    bool wasMounted = true;
    float timeHeadsetRemoved;

    #if !UNITY_EDITOR
    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
                
                string audioService = contextClass.GetStatic<string>("AUDIO_SERVICE");
                audioManager = activity.Call<AndroidJavaObject>("getSystemService", audioService);
            }
        }
    }

    void Update()
    {
        if (audioManager == null) return;

        bool isMicMuted = audioManager.Call<bool>("isMicrophoneMute");
        
        bool isMounted = !isMicMuted;

        if (isMounted != wasMounted)
        {
            if (isMounted)
            {
                AudioListener.pause = false;
                Time.timeScale = 1f;
                Debug.Log("Headset MOUNTED (Mic unmuted by OS)");
                
                if (Time.unscaledTime > timeHeadsetRemoved + timeToReturnToMainScene)
                {
                    Debug.Log("Returning to main scene.");
                    playerTransition.LoadNormalSceneFadeOut(menuSceneName);
                }
            }
            else
            {
                Debug.Log("Headset UNMOUNTED (Mic privacy mute engaged). Timer started.");
                timeHeadsetRemoved = Time.unscaledTime;
                AudioListener.pause = true;
                Time.timeScale = 0f; 
            }

            wasMounted = isMounted;
        }
    }
    #endif
}

using UnityEngine;

public class AndroidHaptics : MonoBehaviour
{
    AndroidJavaObject vibrator;
    AndroidJavaObject activity;
    AndroidJavaObject context;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            context = activity.Call<AndroidJavaObject>("getApplicationContext");
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
#endif
    }

    public void Vibrate(long milliseconds = 100)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = buildVersion.GetStatic<int>("SDK_INT");

                if (sdkInt >= 26)
                {
                    // Use VibrationEffect for API 26+
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot", milliseconds, vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE")
                        );
                        vibrator.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    // Fallback for older APIs
                    vibrator.Call("vibrate", milliseconds);
                }
            }
        }
#endif
    }

    public void VibratePattern()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            long[] pattern = { 0, 100, 200, 300 };

            using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = buildVersion.GetStatic<int>("SDK_INT");

                if (sdkInt >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createWaveform", pattern, -1
                        );
                        vibrator.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", pattern, -1);
                }
            }
        }
#endif
    }
    public void VibrateGradually(int duration)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    if (vibrator != null)
    {
        // Duration of the vibration in milliseconds
        int totalDuration = 4000; // 4 seconds

        // Gradually increasing pattern: Increase intensity over time
        long[] pattern = new long[totalDuration / 100]; // 100ms intervals

        for (int i = 0; i < pattern.Length; i++)
        {
            // Gradually increase the intensity: The pattern value is the vibration duration for each step
            pattern[i] = i * 100; // Increases the intensity (duration) over time
        }

        using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            int sdkInt = buildVersion.GetStatic<int>("SDK_INT");

            if (sdkInt >= 26)
            {
                using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createWaveform", pattern, -1
                    );
                    vibrator.Call("vibrate", vibrationEffect);
                }
            }
            else
            {
                vibrator.Call("vibrate", pattern, -1); // For older devices
            }
        }
    }
#endif
    }


    public void CancelVibration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
            vibrator.Call("cancel");
#endif
    }
}

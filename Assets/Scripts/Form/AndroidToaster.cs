using UnityEngine;
using System.Runtime.InteropServices;

public class AndroidToaster
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static void ShowToast(string message, bool longDuration = false)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                "makeText",
                context,
                message,
                toastClass.GetStatic<int>(longDuration ? "LENGTH_LONG" : "LENGTH_SHORT")
            );
            toast.Call("show");
        }));
    }
#elif UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void HALEON_ShowToast(string message, int duration);

    public static void ShowToast(string message, bool longDuration = false)
    {
        HALEON_ShowToast(message, longDuration ? 6000 : 4000);
    }
#else
    public static void ShowToast(string message, bool longDuration = false)
    {
        Debug.Log("TOAST: " + message); // fallback in Editor
    }
#endif
}

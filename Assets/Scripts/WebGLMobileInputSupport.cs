using UnityEngine;

/// <summary>
/// Ensures native Unity UI input fields request the mobile soft keyboard in a
/// WebGL build. This is intentionally WebGL-only and has no effect in the
/// Editor, Android, or desktop builds.
/// </summary>
public static class WebGLMobileInputSupport
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnableMobileKeyboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.mobileKeyboardSupport = true;
#endif
    }
}

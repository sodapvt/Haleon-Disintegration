using System.Runtime.InteropServices;

public static class WebGLPresentation
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void HALEON_ShowVideoFullscreenPrompt();

    [DllImport("__Internal")]
    private static extern void HALEON_EnterPresentationFullscreen();
#endif

    /// <summary>
    /// Shows the browser-owned fullscreen control when a video sequence begins.
    /// Browsers require the viewer to tap this control before fullscreen or
    /// landscape orientation can be requested.
    /// </summary>
    public static void ShowVideoFullscreenPrompt()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HALEON_ShowVideoFullscreenPrompt();
#endif
    }

    /// <summary>
    /// Requests fullscreen directly from a Unity button tap. Calling this from
    /// the form Submit button satisfies the browser's user-gesture requirement.
    /// </summary>
    public static void EnterFullscreenFromUserGesture()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HALEON_EnterPresentationFullscreen();
#endif
    }
}

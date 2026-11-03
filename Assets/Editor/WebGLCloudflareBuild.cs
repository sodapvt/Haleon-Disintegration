#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLCloudflareBuild
{
    private const string OutputDirectory = "WebGLRelease";

    [MenuItem("Build/HALEON/Build WebGL for Cloudflare")]
    public static void BuildForCloudflare()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/Form.unity",
                "Assets/Scenes/LauncherScene.unity"
            },
            locationPathName = OutputDirectory,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("WebGL build failed. Check the Unity Console for details.");
            return;
        }

        File.WriteAllText(
            Path.Combine(OutputDirectory, "_headers"),
            "/index.html\n  Cache-Control: no-cache\n");

        Debug.Log($"Cloudflare-ready WebGL build created at: {Path.GetFullPath(OutputDirectory)}");
    }
}
#endif

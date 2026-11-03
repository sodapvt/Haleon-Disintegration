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
        // A browser, video preview, or Unity can still have a previous build's
        // StreamingAssets video memory-mapped. Never overwrite that build.
        string outputDirectory = OutputDirectory;
        if (Directory.Exists(outputDirectory) || File.Exists(outputDirectory))
        {
            do
            {
                outputDirectory = OutputDirectory + "-" +
                    System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" +
                    System.Guid.NewGuid().ToString("N").Substring(0, 8);
            }
            while (Directory.Exists(outputDirectory) || File.Exists(outputDirectory));
        }

        Debug.Log($"Building WebGL into a fresh folder: {Path.GetFullPath(outputDirectory)}");

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/Form.unity",
                "Assets/Scenes/LauncherScene.unity"
            },
            locationPathName = outputDirectory,
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
            Path.Combine(outputDirectory, "_headers"),
            "/index.html\n  Cache-Control: no-cache\n");

        Debug.Log($"Cloudflare-ready WebGL build created at: {Path.GetFullPath(outputDirectory)}");
    }
}
#endif

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LinfoWebGLBuild
{
    private const string MainScene = "Assets/_Project/Scenes/SC_LINFO_Invaders_Prototype.unity";

    public static void BuildForItch()
    {
        string outputPath = Environment.GetEnvironmentVariable("LINFO_WEBGL_OUTPUT");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new InvalidOperationException("LINFO_WEBGL_OUTPUT must contain the WebGL output folder.");
        }

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed with result {report.summary.result}.");
        }

        Debug.Log($"WebGL itch.io build completed: {outputPath}");
    }
}

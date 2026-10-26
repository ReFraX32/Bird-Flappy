using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildAutomation
{
    private static readonly string[] Scenes = new[] { "Assets/Scenes/SampleScene.unity" };

    public static void ApplyIcons()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/bird.png");
        if (icon == null)
        {
            Debug.LogError("[BuildAutomation] Failed to load Assets/bird.png for icon!");
            return;
        }

        // Set default application icon (used for Windows standalone exe, taskbar, alt-tab, etc.)
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        // Set Windows Standalone icons
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone, new[] { icon }, IconKind.Any);

        // Set Android icons (standard launcher icon)
        PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Any);

        PlayerSettings.productName = "Bird Flappy";
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildAutomation] App icons successfully applied for Standalone and Android!");
    }

    [MenuItem("Build/Build Windows (PC)")]
    public static void BuildWindows()
    {
        ApplyIcons();

        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
        Directory.CreateDirectory(buildFolder);
        string exePath = Path.Combine(buildFolder, "BirdFlappy.exe");

        Debug.Log($"[BuildAutomation] Starting Windows Standalone build to: {exePath}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildAutomation] Windows build SUCCESS: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s");
        }
        else
        {
            Debug.LogError($"[BuildAutomation] Windows build FAILED: {summary.result} with {summary.totalErrors} errors");
        }
    }

    [MenuItem("Build/Build Android (APK)")]
    public static void BuildAndroid()
    {
        ApplyIcons();

        string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
        Directory.CreateDirectory(buildFolder);
        string apkPath = Path.Combine(buildFolder, "BirdFlappy.apk");

        Debug.Log($"[BuildAutomation] Starting Android build to: {apkPath}");

        string keystorePath = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Keystore", "user.keystore");
        if (File.Exists(keystorePath))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = "flappy123";
            PlayerSettings.Android.keyaliasName = "birdflappy";
            PlayerSettings.Android.keyaliasPass = "flappy123";
            Debug.Log("[BuildAutomation] Custom keystore configured.");
        }
        else
        {
            Debug.LogWarning($"[BuildAutomation] Keystore not found at {keystorePath}");
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildAutomation] Android build SUCCESS: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s");
        }
        else
        {
            Debug.LogError($"[BuildAutomation] Android build FAILED: {summary.result} with {summary.totalErrors} errors");
        }
    }
}

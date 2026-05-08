using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/World.unity"
    };

    // ─── Menú del Editor ──────────────────────────────────────────────────────

    [MenuItem("Build/Build Windows")]
    public static void BuildWindowsMenu() => BuildWindows();

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroidMenu() => BuildAndroid();

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildAndroid();
    }

    // ─── Builds ───────────────────────────────────────────────────────────────

    public static void BuildWindows()
    {
        string outputDir = Path.Combine("Builds", "Windows");
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, "LostWorld.exe");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Run(options, "Windows");
    }

    public static void BuildAndroid()
    {
        string outputDir = Path.Combine("Builds", "Android");
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, "LostWorld.apk");

        // Usar firma de debug para testing
        PlayerSettings.Android.useCustomKeystore = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Run(options, "Android");
    }

    // ─── Modo batch (línea de comandos) ───────────────────────────────────────

    // Uso: Unity.exe -batchmode -executeMethod BuildScript.BatchBuildWindows
    public static void BatchBuildWindows()
    {
        BuildWindows();
        EditorApplication.Exit(0);
    }

    // Uso: Unity.exe -batchmode -executeMethod BuildScript.BatchBuildAndroid
    public static void BatchBuildAndroid()
    {
        BuildAndroid();
        EditorApplication.Exit(0);
    }

    // ─── Utilidad ─────────────────────────────────────────────────────────────

    private static void Run(BuildPlayerOptions options, string platform)
    {
        Debug.Log($"[Build] Iniciando build para {platform}...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[Build] {platform} OK — {summary.totalSize / 1024 / 1024} MB en {summary.totalTime.TotalSeconds:F1}s");
        else
            Debug.LogError($"[Build] {platform} FALLIDO: {summary.totalErrors} error(es)");
    }
}

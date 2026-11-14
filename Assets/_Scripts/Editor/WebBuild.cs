using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Web (WebGL) build for itch.io: Builds/Web/ plus Builds/Fisker-web.zip with index.html at its root, ready to
/// upload as an HTML game. Gzip files that the loader unpacks itself (a host that sends no Content-Encoding header
/// still works), the Default template (no service worker caching an old version), a 1280x720 canvas, a release
/// (non-development) build optimised for runtime speed.
/// Menu: Build > Web (itch.io). Command line:
/// Unity -batchmode -nographics -quit -projectPath . -buildTarget WebGL -executeMethod WebBuild.BuildFromCommandLine
/// </summary>
public static class WebBuild
{
    private const string Folder = "Builds/Web";
    private const string Zip = "Builds/Fisker-web.zip";

    [MenuItem("Build/Web (itch.io)")]
    public static void BuildFromMenu() => Build();

    public static void BuildFromCommandLine() => EditorApplication.Exit(Build() ? 0 : 1);

    private static bool Build()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.productName = "Fisker";
        // A release build optimised for speed (a development / build-times setting left in the editor makes a
        // much larger debug wasm, which wasm-opt can fail on).
        string platform = BuildPipeline.GetBuildTargetName(BuildTarget.WebGL);
        Debug.Log($"Web build settings before: development={EditorUserBuildSettings.development}, code optimization={EditorUserBuildSettings.GetPlatformSettings(platform, "CodeOptimization")}");
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.SetPlatformSettings(platform, "CodeOptimization", "RuntimeSpeed");
        AssetDatabase.SaveAssets();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Folder,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        });
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Web build failed: {report.summary.result}, {report.summary.totalErrors} errors");
            return false;
        }

        if (File.Exists(Zip)) File.Delete(Zip);
        ZipFile.CreateFromDirectory(Folder, Zip, System.IO.Compression.CompressionLevel.Optimal, false);
        Debug.Log($"Web build: {Folder} ({report.summary.totalSize / (1024 * 1024)} MB), zip {Zip} ({new FileInfo(Zip).Length / (1024 * 1024)} MB), scenes: {string.Join(", ", scenes)}");
        return true;
    }
}

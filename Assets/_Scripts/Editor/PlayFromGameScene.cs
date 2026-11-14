using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Play mode always starts from the game scene (the first scene in Build Settings), whichever scene is open in the
/// editor; the open scene comes back when play stops. The prototype scenes have no GameManager and cannot run alone.
/// </summary>
[InitializeOnLoad]
public static class PlayFromGameScene
{
    static PlayFromGameScene() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingEditMode) return;
        var scenes = EditorBuildSettings.scenes;
        EditorSceneManager.playModeStartScene = scenes.Length > 0
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[0].path)
            : null;
    }
}

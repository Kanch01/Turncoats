#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EditorSceneDebug
{
    static EditorSceneDebug()
    {
        // Subscribe to play mode changes
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Only create if GameFlowManager doesn't exist
            if (GameFlowManager.Instance == null)
            {
                // Create a temporary root object
                GameObject rootDebug = new GameObject("debug_Root");

                // Add required components
                rootDebug.AddComponent<GameState>();
                rootDebug.AddComponent<InputLockManager>();
                var gfm = rootDebug.AddComponent<GameFlowManager>();
                gfm.debugMode = true;

                // Don't destroy
                Object.DontDestroyOnLoad(rootDebug);

                Debug.Log("EditorSceneBootstrap: debug_Root created for play-mode testing.");
            }
        }
    }
}
#endif
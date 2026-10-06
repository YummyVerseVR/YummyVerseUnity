using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace YummyVerse.Editor
{
    [InitializeOnLoad]
    internal static class RestaurantStartup
    {
        private const string SessionKey = "YummyVerse.RestaurantOpenedOnStartup";
        private const string ScenePath = "Assets/YummyVerse/Scene/Restaurant.unity";

        static RestaurantStartup()
        {
            if (UnityEngine.Application.isBatchMode || SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.update += OpenWhenReady;
        }

        private static void OpenWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;

            EditorApplication.update -= OpenWhenReady;
            SessionState.SetBool(SessionKey, true);
            // Never replace unsaved work during an Editor restart or domain reload.
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return;

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}

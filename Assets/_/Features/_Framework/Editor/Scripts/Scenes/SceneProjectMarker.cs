using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    [InitializeOnLoad]
    public static class SceneProjectMarker
    {
        static SceneProjectMarker()
        {
            EditorApplication.projectWindowItemOnGUI += DrawMarker;
        }

        private static void DrawMarker(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".unity")) return;

            if (!path.StartsWith("Assets/_/Scenes/ProductScenes/")) return;

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (SceneUsagePoller.UsedScenes.Contains(sceneName))
            {
                Rect r = new Rect(selectionRect.xMax - 20, selectionRect.y, 18, selectionRect.height);

                EditorGUI.DrawRect(r, new Color(0.2f, 1f, 0.2f, 0.3f));
                GUI.Label(r, "●", new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    normal = new GUIStyleState() { textColor = Color.green }
                });
            }
        }
    }
}
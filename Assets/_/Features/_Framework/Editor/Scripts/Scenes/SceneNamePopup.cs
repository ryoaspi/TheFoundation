using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    public class SceneNamePopup : EditorWindow
    {
        private string _name;
        private SceneType _type;

        public static void Show(SceneType type)
        {
            var window = CreateInstance<SceneNamePopup>();

            window._type = type;

            window.titleContent = new GUIContent("Create Scene");

            window.minSize = new Vector2(250, 80);
            window.maxSize = new Vector2(250, 80);

            window.ShowUtility();
        }

        
        private void OnGUI()
        {
            GUILayout.Space(8);

            GUILayout.Label("Scene Name", EditorStyles.boldLabel);

            GUI.SetNextControlName("SceneNameField");

            _name = EditorGUILayout.TextField(_name);

            GUILayout.Space(8);

            if (GUILayout.Button("Create"))
            {
                SceneCreationTool.CreateSceneWithName(_type, _name);

                EditorApplication.delayCall += Close;
            }

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("SceneNameField");
            }
            
        }
    }
}
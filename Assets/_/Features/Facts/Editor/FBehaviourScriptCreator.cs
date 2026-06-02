using System.IO;
using log4net.Filter;
using UnityEditor;
using UnityEngine;

namespace TheFundation.Editor
{
    public class FBehaviourScriptCreator : EditorWindow
    {
        private string _scriptName = "NewBehaviour";
        private string _targetFolder = "Assets";
        private string _namespaceName = "Features";

        [MenuItem("Tools/FBehaviour/Create FBehaviour Script")]
        private static void ShowWindow()
        {
            GetWindow<FBehaviourScriptCreator>("Create FBehaviour Script");
        }

        private void OnGUI()
        {
            GUILayout.Label("FBehaviour Script Generator", EditorStyles.boldLabel);
            
            _scriptName = EditorGUILayout.TextField("Script Name",_scriptName);
            _namespaceName = EditorGUILayout.TextField("Namespace",_namespaceName);
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Output Folder");
            EditorGUILayout.BeginHorizontal();
            _targetFolder = EditorGUILayout.TextField(_targetFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected) && selected.Contains("Assets"))
                {
                    _targetFolder = "Assets" + selected.Replace(Application.dataPath, "");
                }
            }
            
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Scritp"))
            {
                if (string.IsNullOrWhiteSpace(_scriptName))
                {
                    EditorUtility.DisplayDialog("Error", "Script name cannot be empty", "OK");
                    return;
                }

                CreateScript(_scriptName.Trim(), _namespaceName.Trim(), _targetFolder);
            }
        }

        private void CreateScript(string name, string ns, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Error", "Folder doesn't exist", "OK");
                return;
            }

            string path = Path.Combine(folder, $"{name}.cs");

            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("error", $"Script already exists: \n {path}", "OK");
                return;
            }
            
            string script =
                $@"using UnityEngine;
using TheFundation.Runtime;

namespace {ns}
{{
    public class {name} : FBehaviour
    {{
        #region Fields

        #endregion


        #region Initialization

        protected override void OnInit()
        {{
            // Called once on creation
        }}

        #endregion


        #region Update Loop

        protected override void OnUpdate()
        {{
            // Called every frame
        }}

        protected override void OnFixedUpdate()
        {{
            // Physics updates
        }}

        #endregion


        #region Events

        protected override void OnEnabled()
        {{
        }}

        protected override void OnDisabled()
        {{
        }}

        protected override void OnDestroyed()
        {{
        }}

        #endregion


        #region Private API

        #endregion
    }}
}}";

            File.WriteAllText(path, script);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Script '{name}' created successfully.", "OK");

        }
    }
}

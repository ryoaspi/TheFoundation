using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheFundation.Editor
{
    public class FeatureModuleCreator : EditorWindow
    {
        private string moduleName = "NewFeature";

        [MenuItem("Tools/Feature/Create Feature Module")]
        private static void ShowWindow()
        {
            GetWindow<FeatureModuleCreator>("Create Feature Module");
        }

        private void OnGUI()
        {
            GUILayout.Label("Feature Module Generator", EditorStyles.boldLabel);

            moduleName = EditorGUILayout.TextField("Module Name", moduleName);

            if (GUILayout.Button("Create"))
            {
                if (string.IsNullOrWhiteSpace(moduleName))
                {
                    EditorUtility.DisplayDialog("Error", "Module name is empty.", "OK");
                    return;
                }

                CreateFeatureModule(moduleName.Trim());
            }
        }

        private void CreateFeatureModule(string name)
        {
            // --- Corrected folder structure ---
            string basePath = "Assets/_Features";
            string featurePath = Path.Combine(basePath, name);
            string runtimePath = Path.Combine(featurePath, "Runtime");
            string editorPath = Path.Combine(featurePath, "Editor");

            // Create folders
            CreateFolderIfNotExist("Assets", "_Features");
            CreateFolderIfNotExist(basePath, name);
            CreateFolderIfNotExist(featurePath, "Runtime");
            CreateFolderIfNotExist(featurePath, "Editor");

            // Create ASMDEFs
            CreateAsmDef(Path.Combine(runtimePath, $"{name}.Runtime.asmdef"), GetRuntimeAsmdefJSON(name));
            CreateAsmDef(Path.Combine(editorPath, $"{name}.Editor.asmdef"), GetEditorAsmdefJSON(name));

            // Create default script inheriting from FBehaviour
            CreateBaseScript(name, runtimePath);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                $"Feature Module '{name}' created successfully.",
                "OK"
            );
        }

        private void CreateFolderIfNotExist(string parent, string folderName)
        {
            string fullPath = Path.Combine(parent, folderName);
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        private void CreateAsmDef(string path, string json)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, json);
        }

        // ----------------- ASMDEF JSON ---------------------

        private string GetRuntimeAsmdefJSON(string name)
        {
            return
$@"{{
    ""name"": ""{name}.Runtime"",
    ""references"": [
        ""TheFundation.Runtime""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [ ""Editor"" ],
    ""autoReferenced"": true
}}";
        }

        private string GetEditorAsmdefJSON(string name)
        {
            return
$@"{{
    ""name"": ""{name}.Editor"",
    ""references"": [
        ""{name}.Runtime"",
        ""TheFundation.Editor""
    ],
    ""includePlatforms"": [ ""Editor"" ],
    ""autoReferenced"": true
}}";
        }

        // ----------------- SCRIPT TEMPLATE ---------------------

        private void CreateBaseScript(string moduleName, string runtimePath)
        {
            string scriptName = moduleName + "Main.cs";
            string fullPath = Path.Combine(runtimePath, scriptName);

            if (File.Exists(fullPath))
                return;

            string script =
$@"using TheFundation.Runtime;
using UnityEngine;

namespace Features.{moduleName}
{{
    public class {moduleName} : FBehaviour
    {{
        protected override void OnInit()
        {{
            // Initialize your feature
        }}
    }}
}}";

            File.WriteAllText(fullPath, script);
        }
    }
}

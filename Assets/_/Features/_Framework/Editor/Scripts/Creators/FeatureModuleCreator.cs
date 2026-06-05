using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    //TODO Je le note pour ne pas oublier, mais à priori une architecture de dossier pour Editor
    //serait plus interessante:
    //Tools : Pour ce qui est outil de visualisation ou d'analyse
    //Creators : Pour ce qui génère des ressources/assets (FeatureCreator)
    //Inspectors : Pour ce qui modifie/touche à l'inspector d'un script de facon automatique (CustomEditor(Typeof))
    public class FeatureModuleCreator : EditorWindow
    {
        private string moduleName = "NewFeature";

        [MenuItem("TheFoundation/Create/Feature Module")]
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
            string basePath = Path.Combine("Assets", "_", "Features");
            string featurePath = Path.Combine(basePath, name);
            string runtimePath = Path.Combine(featurePath, "Runtime");
            string editorPath = Path.Combine(featurePath, "Editor");
            string scriptsPath = Path.Combine(runtimePath, "Scripts");

            // Directory.CreateDirectory est récursif — crée tout d'un coup
            Directory.CreateDirectory(scriptsPath);
            Directory.CreateDirectory(editorPath);

            // Create ASMDEFs
            CreateAsmDef(Path.Combine(runtimePath, $"{name}.Runtime.asmdef"), GetRuntimeAsmdefJSON(name));
            CreateAsmDef(Path.Combine(editorPath, $"{name}.Editor.asmdef"), GetEditorAsmdefJSON(name));

            // Create default script inheriting from FBehaviour
            CreateBaseScript(name, Path.Combine(runtimePath,"Scripts"));

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog(
                "Success",
                $"Feature Module '{name}' created successfully.",
                "OK"
            );
        }

        /*
         * Le fait de passer par AssetDatabase, 
         */
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
    ""rootNamespace"": ""{name}.Runtime"",
    ""references"": [
        ""TheFoundation.Runtime""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""autoReferenced"": true
}}";
        }

        private string GetEditorAsmdefJSON(string name)
        {
            return
$@"{{
    ""name"": ""{name}.Editor"",
    ""rootNamespace"": ""{name}.Editor"",
    ""references"": [
        ""{name}.Runtime""
    ],
    ""includePlatforms"": [ ""Editor"" ],
    ""autoReferenced"": true
}}";
        }

        // ----------------- SCRIPT TEMPLATE ---------------------

        private void CreateBaseScript(string moduleName, string runtimePath)
        {
            string scriptName = moduleName + "Behaviour.cs";
            string fullPath = Path.Combine(runtimePath, scriptName);

            if (File.Exists(fullPath))
                return;

            string script = $@"
using TheFoundation.Runtime;
using UnityEngine;

namespace {moduleName}.Runtime
{{
    public class {moduleName}Behaviour : FBehaviour
    {{

        public void Start()
        {{

        }}
        /*
        protected override void OnInit()
        {{
            // Initialize your feature
        }}
        */
    }}
}}";

            File.WriteAllText(fullPath, script);
            AssetDatabase.Refresh();
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;

namespace TheFoundation.Editor
{
    
    public static class SceneCreationTool
    {
        [MainToolbarElement("Scene Creation")]
        private static MainToolbarElement CreateSceneToolbarButton()
        {
            var content = new MainToolbarContent { text = "New Scene" };

            var toolbarButton = new MainToolbarButton(content, ShowSceneMenu);

            toolbarButton.populateContextMenu += (DropdownMenu menu) => { };

            return toolbarButton;
        }


        #region Scenes Management

        private static void ShowSceneMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Product Scene"), false, () => OpenNamePopup(SceneType.Product));
            menu.AddItem(new GUIContent("Contributor Scene"), false, () => OpenNamePopup(SceneType.Contributor));
            menu.AddItem(new GUIContent("Debug Scene"), false, () => OpenNamePopup(SceneType.Debug));

            menu.ShowAsContext();
        }

        private static void OpenNamePopup(SceneType type)
        {
            SceneNamePopup.Show(type);
        }

        public static void CreateSceneWithName(SceneType type, string name)
        {
            string root = GetRoot(type);
            EnsureFolderExists(root);

            string finalName = (type == SceneType.Product)
                ? GenerateSceneName(root, name)
                : name;

            string path = $"{root}/{finalName}.unity";

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single
            );

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();

            EditorApplication.delayCall += () =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            };
        }

        #endregion

        #region Specific Scenes Creation Methods

        private static string GetRoot(SceneType type)
        {
            return type switch
            {
                SceneType.Product => _productSceneRoot,
                SceneType.Contributor => _contributorSceneRoot,
                SceneType.Debug => _debugSceneRoot,
                _ => _productSceneRoot
            };
        }

        #endregion

        #region Utils

        private static int GetNextSceneIndex(string rootFolder)
        {
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { rootFolder });

            int maxIndex = -1;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);

                if (string.IsNullOrEmpty(name))
                    continue;

                var split = name.Split('_');
                if (split.Length == 0)
                    continue;

                if (!int.TryParse(split[0], out int index))
                    continue;

                if (index >= 90 && index <= 99)
                    continue;

                if (index > maxIndex)
                    maxIndex = index;
            }

            return maxIndex + 1;
        }

        private static string GenerateSceneName(string rootFolder, string customName)
        {
            int index = GetNextSceneIndex(rootFolder);
            return $"{index:00}_{customName}";
        }

        private static void EnsureFolderExists(string fullPath)
        {
            string normalized = fullPath.Replace("\\", "/");

            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string parent = Path.GetDirectoryName(normalized).Replace("\\", "/");
            string folderName = Path.GetFileName(normalized);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        #endregion

        #region Privates

        private static string _contributorSceneRoot = Path.Combine("Assets", "_", "Scenes", "ContributorScenes");
        private static string _productSceneRoot = Path.Combine("Assets", "_", "Scenes", "ProductScenes");
        private static string _debugSceneRoot = Path.Combine("Assets", "_", "Scenes", "DebugScenes");

        #endregion
    }

    public enum SceneType
    {
        Product,
        Contributor,
        Debug
    }
}
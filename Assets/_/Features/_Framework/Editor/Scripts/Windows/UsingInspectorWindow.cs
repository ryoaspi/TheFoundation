using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class StringSearchInFeaturesWindow : EditorWindow
{
    private const string RootFolder = "Assets/_/Features";
    private string _search = "";
    private Vector2 _scroll;
    private readonly List<string> _results = new();

    [MenuItem("TheFoundation/Tools/Search String In Features")]
    public static void Open()
    {
        GetWindow<StringSearchInFeaturesWindow>("String Search");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Folder", RootFolder);
        _search = EditorGUILayout.TextField("Search", _search);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_search)))
        {
            if (GUILayout.Button("Scan"))
                Scan();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Matches: {_results.Count}", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var path in _results)
        {
            if (GUILayout.Button(path, EditorStyles.linkLabel))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (asset != null)
                    Selection.activeObject = asset;
                else
                    EditorUtility.RevealInFinder(path);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _results.Clear();

        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            Debug.LogError($"Folder not found: {RootFolder}");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { RootFolder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
                continue;

            var text = File.ReadAllText(fullPath);
            if (text.Contains(_search))
                _results.Add(path);
        }

        _results.Sort();
        Repaint();
    }
}
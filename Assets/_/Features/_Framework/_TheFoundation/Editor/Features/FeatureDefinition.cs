using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


//TODO See if its possible to place de FeatureDefinitions SO's into a folder instead of the feature folder itself
//But I'm tired, so it will be for later.
//
// ─────────────────────────────────────────────────────────────
// FEATURE DEFINITION — ScriptableObject
// ─────────────────────────────────────────────────────────────
namespace TheFundation.Editor
{
    public enum FeatureType
    {
        Gameplay,
        UI,
        Audio,
        Shared,
        Tool,
        System,
        Foundation,
    }

    [CreateAssetMenu(fileName = "FeatureDefinition", menuName = "Foundation/Feature Definition")]
    public class FeatureDefinition : ScriptableObject
    {
        public string m_ID;
        public string m_title;
        public FeatureType m_category;
        [TextArea(4, 12)]
        public string m_description;
        public string m_version;
        public string m_author;
    }

    // ─────────────────────────────────────────────────────────────
    // FEATURE DATABASE — finds all FeatureDefinition assets
    // ─────────────────────────────────────────────────────────────
    public static class FeatureDatabase
    {
        public static List<FeatureDefinition> LoadAll()
        {
            // Sort by asset creation date (oldest first) via file system
            return AssetDatabase.FindAssets("t:FeatureDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => File.GetCreationTime(path))
                .Select(AssetDatabase.LoadAssetAtPath<FeatureDefinition>)
                .Where(f => f != null)
                .ToList();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // FEATURE REGISTRY — per-user persistence via EditorPrefs
    // ─────────────────────────────────────────────────────────────
    public static class FeatureRegistry
    {
        private const string KEY = "TheFoundation.FeatureRegistry";

        [Serializable]
        private class RegistryData { public List<string> seen = new(); }

        private static RegistryData _data;

        private static RegistryData Data
        {
            get
            {
                if (_data != null) return _data;
                _data = EditorPrefs.HasKey(KEY)
                    ? JsonUtility.FromJson<RegistryData>(EditorPrefs.GetString(KEY))
                    : new RegistryData();
                return _data;
            }
        }

        public static bool IsSeen(string id) => Data.seen.Contains(id);

        public static void MarkSeen(string id)
        {
            if (!Data.seen.Contains(id)) Data.seen.Add(id);
            EditorPrefs.SetString(KEY, JsonUtility.ToJson(Data));
        }

        public static void MarkAllSeen(IEnumerable<FeatureDefinition> features)
        {
            foreach (var f in features) if (!Data.seen.Contains(f.m_ID)) Data.seen.Add(f.m_ID);
            EditorPrefs.SetString(KEY, JsonUtility.ToJson(Data));
        }

        public static void Reset()
        {
            EditorPrefs.DeleteKey(KEY);
            _data = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // BOOTSTRAP — shows popup on editor load if unseen features exist
    // ─────────────────────────────────────────────────────────────
    [InitializeOnLoad]
    public static class FeatureBootstrap
    {
        static FeatureBootstrap()
        {
            EditorApplication.delayCall += Check;
        }

        private static void Check()
        {
            var unseen = FeatureDatabase.LoadAll()
                .Where(f => !string.IsNullOrEmpty(f.m_ID) && !FeatureRegistry.IsSeen(f.m_ID))
                .ToList();

            if (unseen.Count > 0)
                FeaturePopupWindow.Show(unseen);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // POPUP WINDOW
    // ─────────────────────────────────────────────────────────────
    public class FeaturePopupWindow : EditorWindow
    {
        private List<FeatureDefinition> _features;
        private int _index;
        private Vector2 _scroll;

        public static void Show(List<FeatureDefinition> features)
        {
            var w = GetWindow<FeaturePopupWindow>(true, "New Feature", true);
            w.minSize = new Vector2(420, 340);
            w.maxSize = new Vector2(420, 340);
            w._features = features;
            w._index = 0;
            w.ShowUtility();
        }

        private FeatureDefinition Current => _features != null && _index < _features.Count
            ? _features[_index]
            : null;

        private void OnGUI()
        {
            if (Current == null) { Close(); return; }

            var f = Current;

            // ── Header ──
            EditorGUILayout.Space(8);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 , normal = {textColor = new Color(0.3f, 0.85f, 0.5f)}};
            EditorGUILayout.LabelField(f.m_title, titleStyle);

            var metaStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.8f, 1f) } };
            EditorGUILayout.LabelField($"{f.m_category}   v{f.m_version}   {f.m_author}", metaStyle);

            EditorGUILayout.Space(6);
            DrawSeparator();
            EditorGUILayout.Space(4);

            // ── Description ──
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
            var descStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12 };
            EditorGUILayout.LabelField(f.m_description, descStyle);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(8);

            // ── Counter ──
            var counterStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField($"{_index + 1} / {_features.Count}", counterStyle);
            EditorGUILayout.Space(4);

            // ── Buttons ──
            EditorGUILayout.BeginHorizontal();

            var gotItStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.white } };
            var prevBg = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("Got it ✓", gotItStyle, GUILayout.Height(28)))
                OnGotIt();

            GUI.backgroundColor = new Color(1f, 0.6f, 0.1f);
            if (GUILayout.Button("Later", gotItStyle, GUILayout.Height(28)))
                OnLater();

            GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f);
            if (GUILayout.Button("Skip all", gotItStyle, GUILayout.Height(28)))
                OnSkipAll();

            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.1f));
        }

        private void OnGotIt()
        {
            FeatureRegistry.MarkSeen(Current.m_ID);
            _index++;
            if (_index >= _features.Count) Close();
            else Repaint();
        }

        private void OnLater()
        {
            // Skip current without marking seen — advance to next, close if last
            _index++;
            if (_index >= _features.Count) Close();
            else Repaint();
        }

        private void OnSkipAll()
        {
            FeatureRegistry.MarkAllSeen(_features);
            Close();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // CUSTOM INSPECTOR — replaces the broken GPT version  
    // ─────────────────────────────────────────────────────────────
    [CustomEditor(typeof(FeatureDefinition))]
    public class FeatureDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // DrawDefaultInspector handles serialization correctly,
            // including the [TextArea] attribute on m_description.
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("Registry", EditorStyles.boldLabel);

            var def = (FeatureDefinition)target;
            bool seen = !string.IsNullOrEmpty(def.m_ID) && FeatureRegistry.IsSeen(def.m_ID);

            var prevColor = GUI.backgroundColor;

            if (seen)
            {
                EditorGUILayout.HelpBox("✓ Marked as seen by this user.", MessageType.Info);
                GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f);
                if (GUILayout.Button("Reset seen state"))
                    FeatureRegistry.Reset();
            }
            else
            {
                EditorGUILayout.HelpBox("Not yet seen by this user.", MessageType.Warning);
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                if (GUILayout.Button("Mark as seen"))
                    FeatureRegistry.MarkSeen(def.m_ID);
            }

            GUI.backgroundColor = prevColor;

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.3f, 0.5f, 1f);
            if (GUILayout.Button("Preview popup"))
                FeaturePopupWindow.Show(new List<FeatureDefinition> { def });

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button("Clear all seen (reset registry)"))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset registry",
                    "This will clear all seen definitions for this user. All popups will show again on next load.",
                    "Reset", "Cancel"))
                {
                    FeatureRegistry.Reset();
                }
            }
            GUI.backgroundColor = prevColor;
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.15f));
        }
    }
}
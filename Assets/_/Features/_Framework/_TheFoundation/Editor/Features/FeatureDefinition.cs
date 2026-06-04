using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


// TODO
// Change the name of the "FeatureDefinition", its confusing, rather go for ChangeLogEntry or ProjectNote
// This will undoubtedly corrupt the data of the previous created "Notes".


// ─────────────────────────────────────────────────────────────
// FEATURE DEFINITION — ScriptableObject
// ─────────────────────────────────────────────────────────────
namespace TheFundation.Editor
{
    public enum FeatureType { Gameplay, UI, Audio, Shared, Tool, System, Foundation }

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
        public string m_updatedAt;
        public List<Texture2D> m_gallery = new();
    }

    // ─────────────────────────────────────────────────────────────
    // FEATURE DATABASE
    // ─────────────────────────────────────────────────────────────
    public static class FeatureDatabase
    {
        public static List<FeatureDefinition> LoadAll()
        {
            return AssetDatabase.FindAssets("t:FeatureDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => File.GetCreationTime(path + ".meta"))
                .Select(AssetDatabase.LoadAssetAtPath<FeatureDefinition>)
                .Where(f => f != null)
                .ToList();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // FEATURE REGISTRY — per-user via EditorPrefs
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

        public static void UnmarkSeen(string id)
        {
            if (Data.seen.Contains(id)) Data.seen.Remove(id);
            EditorPrefs.SetString(KEY, JsonUtility.ToJson(Data));
        }
        
        public static void MarkAllSeen(IEnumerable<FeatureDefinition> features)
        {
            foreach (var f in features)
                if (!Data.seen.Contains(f.m_ID)) Data.seen.Add(f.m_ID);
            EditorPrefs.SetString(KEY, JsonUtility.ToJson(Data));
        }

        public static void Reset()
        {
            EditorPrefs.DeleteKey(KEY);
            _data = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // BOOTSTRAP
    // ─────────────────────────────────────────────────────────────
    [InitializeOnLoad]
    public static class FeatureBootstrap
    {
        static FeatureBootstrap() => EditorApplication.delayCall += Check;

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
    // IMAGE PREVIEW WINDOW
    // ─────────────────────────────────────────────────────────────
    public class ImagePreviewWindow : EditorWindow
    {
        private Texture2D _image;

        public static void Show(Texture2D image)
        {
            var w = GetWindow<ImagePreviewWindow>(true, "Preview", true);
            w._image = image;
            float ratio = (float)image.height / image.width;
            float width = Mathf.Min(700f, Screen.currentResolution.width * 0.8f);
            w.minSize = new Vector2(width, width * ratio);
            w.maxSize = w.minSize;
            w.ShowUtility();
        }

        private void OnGUI()
        {
            if (_image == null) { Close(); return; }
            var rect = GUILayoutUtility.GetRect(position.width, position.height);
            GUI.DrawTexture(rect, _image, ScaleMode.ScaleToFit);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // POPUP WINDOW
    // ─────────────────────────────────────────────────────────────
    public class FeaturePopupWindow : EditorWindow
    {
        private List<FeatureDefinition> _features;
        private int _index;
        private int _galleryIndex;
        private bool _showGallery;
        private Vector2 _scroll;

        public static void Show(List<FeatureDefinition> features)
        {
            var w = GetWindow<FeaturePopupWindow>(true, "New Feature", true);
            w.minSize = new Vector2(420, 380);
            w.maxSize = new Vector2(420, 380);
            w._features = features;
            w._index = 0;
            w._showGallery = false;
            w.ShowUtility();
        }

        private FeatureDefinition Current => _features != null && _index < _features.Count
            ? _features[_index] : null;

        private void OnGUI()
        {
            if (Current == null) { Close(); return; }
            var f = Current;
            bool hasGallery = f.m_gallery != null && f.m_gallery.Count > 0;

            // ── Header ──
            EditorGUILayout.Space(8);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 15, normal = { textColor = new Color(0.3f, 0.85f, 0.5f) } };
            EditorGUILayout.LabelField(f.m_title, titleStyle);

            var metaStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.6f, 0.8f, 1f) } };
            EditorGUILayout.LabelField($"{f.m_category}   v{f.m_version}   {f.m_author}", metaStyle);

            EditorGUILayout.Space(6);
            DrawSeparator();
            EditorGUILayout.Space(4);

            if (!_showGallery)
            {
                // ── Description ──
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
                var descStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12 };
                EditorGUILayout.LabelField(f.m_description, descStyle);
                EditorGUILayout.EndScrollView();

                // ── Gallery toggle ──
                if (hasGallery)
                {
                    EditorGUILayout.Space(4);
                    GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
                    if (GUILayout.Button($"Show Gallery  ({f.m_gallery.Count} image{(f.m_gallery.Count > 1 ? "s" : "")})", GUILayout.Height(24)))
                    {
                        _showGallery = true;
                        _galleryIndex = 0;
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                // ── Gallery view ──
                var image = f.m_gallery[_galleryIndex];
                if (image != null)
                {
                    var rect = GUILayoutUtility.GetRect(380, 150, GUILayout.ExpandWidth(true));
                    GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit);

                    // Click → preview
                    if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                    {
                        ImagePreviewWindow.Show(image);
                        Event.current.Use();
                    }

                    var hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontStyle = FontStyle.Italic };
                    EditorGUILayout.LabelField("Click to enlarge", hintStyle);
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = _galleryIndex > 0;
                if (GUILayout.Button("<", GUILayout.Width(40))) _galleryIndex--;
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{_galleryIndex + 1} / {f.m_gallery.Count}", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUI.enabled = _galleryIndex < f.m_gallery.Count - 1;
                if (GUILayout.Button(">", GUILayout.Width(40))) _galleryIndex++;
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                if (GUILayout.Button("← Back to description", GUILayout.Height(22)))
                    _showGallery = false;
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(6);

            // ── Date + counter ──
            if (!string.IsNullOrEmpty(f.m_updatedAt))
            {
                var dateStyle = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.8f, 0.6f, 0.3f) } };
                EditorGUILayout.LabelField($"Updated: {f.m_updatedAt}", dateStyle);
            }

            var counterStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField($"{_index + 1} / {_features.Count}", counterStyle);
            EditorGUILayout.Space(4);

            // ── Buttons ──
            EditorGUILayout.BeginHorizontal();
            var gotItStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.white } };
            var prevBg = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("Got it ✓", gotItStyle, GUILayout.Height(28))) OnGotIt();

            GUI.backgroundColor = new Color(1f, 0.6f, 0.1f);
            if (GUILayout.Button("Later", gotItStyle, GUILayout.Height(28))) OnLater();

            GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f);
            if (GUILayout.Button("Skip all", gotItStyle, GUILayout.Height(28))) OnSkipAll();

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
            _showGallery = false;
            _index++;
            if (_index >= _features.Count) Close();
            else Repaint();
        }

        private void OnLater()
        {
            _showGallery = false;
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
    // CUSTOM INSPECTOR
    // ─────────────────────────────────────────────────────────────
    [CustomEditor(typeof(FeatureDefinition))]
    public class FeatureDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
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
                    FeatureRegistry.UnmarkSeen(def.m_ID);
            }
            else
            {
                EditorGUILayout.HelpBox("Not yet seen by this user.", MessageType.Warning);
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                if (GUILayout.Button("Mark as seen")) FeatureRegistry.MarkSeen(def.m_ID);
            }

            GUI.backgroundColor = prevColor;

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f);
            if (GUILayout.Button("Set updated at → now"))
            {
                def.m_updatedAt = DateTime.Now.ToString("yyyy-MM-dd");
                EditorUtility.SetDirty(def);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.3f, 0.5f, 1f);
            if (GUILayout.Button("Preview popup"))
                FeaturePopupWindow.Show(new List<FeatureDefinition> { def });

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button("Clear all seen (reset registry)"))
            {
                if (EditorUtility.DisplayDialog("Reset registry",
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
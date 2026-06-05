using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    public class ProjectNotePopupWindow : EditorWindow
    {
        public static void Show(List<ProjectNoteDefinition> projectNotes)
        {
            var w = GetWindow<ProjectNotePopupWindow>(true, "New Project Note !", true);
            w.minSize = new Vector2(420, 380);
            w.maxSize = new Vector2(420, 380);
            w._projectNotes = projectNotes;
            w._index = 0;
            w._showGallery = false;
            w.ShowUtility();
        }

        private ProjectNoteDefinition Current => _projectNotes != null && _index < _projectNotes.Count
            ? _projectNotes[_index] : null;

        private void OnGUI()
        {
            if (Current == null) { Close(); return; }
            var pn = Current;
            bool hasGallery = pn.m_gallery != null && pn.m_gallery.Count > 0;

            // ── Header ──
            EditorGUILayout.Space(8);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 15, normal = { textColor = new Color(0.3f, 0.85f, 0.5f) } };
            EditorGUILayout.LabelField(pn.m_title, titleStyle);

            var metaStyle = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.6f, 0.8f, 1f) } };
            EditorGUILayout.LabelField($"{pn.m_category}   v{pn.m_version}   {pn.m_author}", metaStyle);

            EditorGUILayout.Space(6);
            DrawSeparator();
            EditorGUILayout.Space(4);

            if (!_showGallery)
            {
                // ── Description ──
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
                var descStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 12 };
                EditorGUILayout.LabelField(pn.m_description, descStyle);
                EditorGUILayout.EndScrollView();

                // ── Gallery toggle ──
                if (hasGallery)
                {
                    EditorGUILayout.Space(4);
                    GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
                    if (GUILayout.Button($"Show Gallery  ({pn.m_gallery.Count} image{(pn.m_gallery.Count > 1 ? "s" : "")})", GUILayout.Height(24)))
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
                var image = pn.m_gallery[_galleryIndex];
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
                GUILayout.Label($"{_galleryIndex + 1} / {pn.m_gallery.Count}", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUI.enabled = _galleryIndex < pn.m_gallery.Count - 1;
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
            if (!string.IsNullOrEmpty(pn.m_updatedAt))
            {
                var dateStyle = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.8f, 0.6f, 0.3f) } };
                EditorGUILayout.LabelField($"Updated: {pn.m_updatedAt}", dateStyle);
            }

            var counterStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField($"{_index + 1} / {_projectNotes.Count}", counterStyle);
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
            ProjectNoteRegistry.MarkSeen(Current.m_ID);
            _showGallery = false;
            _index++;
            if (_index >= _projectNotes.Count) Close();
            else Repaint();
        }

        private void OnLater()
        {
            _showGallery = false;
            _index++;
            if (_index >= _projectNotes.Count) Close();
            else Repaint();
        }

        private void OnSkipAll()
        {
            ProjectNoteRegistry.MarkAllSeen(_projectNotes);
            Close();
        }
        
        
        private List<ProjectNoteDefinition> _projectNotes;
        private int _index;
        private int _galleryIndex;
        private bool _showGallery;
        private Vector2 _scroll;
    }
}

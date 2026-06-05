using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    [CustomEditor(typeof(ProjectNoteDefinition))]
    public class ProjectNoteDefinitionCustomEditor : UnityEditor.Editor
    {
       public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Registry", EditorStyles.boldLabel);

            var def = (ProjectNoteDefinition)target;
            bool seen = !string.IsNullOrEmpty(def.m_ID) && ProjectNoteRegistry.IsSeen(def.m_ID);
            var prevColor = GUI.backgroundColor;

            if (seen)
            {
                EditorGUILayout.HelpBox("✓ Marked as seen by this user.", MessageType.Info);
    
                GUI.backgroundColor = new Color(0.75f, 0.2f, 0.2f);
                if (GUILayout.Button("Reset seen state"))
                    ProjectNoteRegistry.UnmarkSeen(def.m_ID);
            }
            else
            {
                EditorGUILayout.HelpBox("Not yet seen by this user.", MessageType.Warning);
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                if (GUILayout.Button("Mark as seen")) ProjectNoteRegistry.MarkSeen(def.m_ID);
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
                ProjectNotePopupWindow.Show(new List<ProjectNoteDefinition> { def });

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUILayout.Button("Clear all seen (reset registry)"))
            {
                if (EditorUtility.DisplayDialog("Reset registry",
                    "This will clear all seen definitions for this user. All popups will show again on next load.",
                    "Reset", "Cancel"))
                {
                    ProjectNoteRegistry.Reset();
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

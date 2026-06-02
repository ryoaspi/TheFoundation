#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TheFundation.Runtime;

namespace TheFundation.Editor
{
    public class SettingsAuditEditor : EditorWindow
    {
        private SettingsDefinitionCollection _collection;

        [MenuItem("TheFundation/Tools/Settings Audit")]
        public static void Open()
        {
            var win = GetWindow<SettingsAuditEditor>();
            win.titleContent = new GUIContent("Settings Audit");
            win.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SETTINGS AUDIT", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Outil de validation des SettingDefinition.\n" +
                "Évite les erreurs communes et garantit une qualité professionnelle.",
                MessageType.Info);

            EditorGUILayout.Space();

            _collection = (SettingsDefinitionCollection)EditorGUILayout.ObjectField(
                "Settings Collection",
                _collection,
                typeof(SettingsDefinitionCollection),
                false
            );

            if (_collection == null)
            {
                EditorGUILayout.HelpBox("Veuillez sélectionner un SettingsDefinitionCollection.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Lancer l'audit", GUILayout.Height(30)))
            {
                RunAudit();
            }
        }


        private void RunAudit()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Résultats de l'audit :");
            EditorGUILayout.Space();

            var settings = _collection.m_Settings;
            var keys = new HashSet<string>();
            var used = new HashSet<string>();
            int errorCount = 0;
            int warnCount = 0;

            if (settings == null || settings.Length == 0)
            {
                EditorGUILayout.HelpBox("Aucun SettingDefinition dans la collection.", MessageType.Error);
                return;
            }

            foreach (var def in settings)
            {
                if (def == null)
                {
                    EditorGUILayout.HelpBox("Un SettingDefinition est null dans la collection.", MessageType.Error);
                    errorCount++;
                    continue;
                }

                EditorGUILayout.LabelField($"▶ {def.name}", EditorStyles.boldLabel);

                // -------- KEY CHECK ---------
                if (string.IsNullOrEmpty(def.m_key))
                {
                    EditorGUILayout.HelpBox("Clé m_Key manquante.", MessageType.Error);
                    errorCount++;
                }
                else
                {
                    if (!keys.Add(def.m_key))
                    {
                        EditorGUILayout.HelpBox($"Clé dupliquée : {def.m_key}", MessageType.Error);
                        errorCount++;
                    }
                }

                // -------- LABEL CHECK ---------
                if (string.IsNullOrEmpty(def.m_labelKey))
                {
                    EditorGUILayout.HelpBox("LabelKey manquant.", MessageType.Warning);
                    warnCount++;
                }

                if (string.IsNullOrEmpty(def.m_descriptionKey))
                {
                    EditorGUILayout.HelpBox("DescriptionKey manquant.", MessageType.Warning);
                    warnCount++;
                }

                // -------- FLOAT CHECK ---------
                if (def.m_type == SettingType.Float)
                {
                    if (def.m_minFloat > def.m_maxFloat)
                    {
                        EditorGUILayout.HelpBox("Range invalide : min > max", MessageType.Error);
                        errorCount++;
                    }

                    if (def.m_defaultFloat < def.m_minFloat || def.m_defaultFloat > def.m_maxFloat)
                    {
                        EditorGUILayout.HelpBox("DefaultValue hors range (min/max).", MessageType.Error);
                        errorCount++;
                    }
                }

                // -------- DROPDOWN CHECK ---------
                if (def.m_type == SettingType.Dropdown)
                {
                    if (def.m_options == null || def.m_options.Length == 0)
                    {
                        EditorGUILayout.HelpBox("Dropdown sans options.", MessageType.Error);
                        errorCount++;
                    }
                }

                EditorGUILayout.Space();
            }

            // -------- UI Check ---------
            var fbehaviours = GameObject.FindObjectsOfType<FBehaviour>(true);
            foreach (var fb in fbehaviours)
            {
                var ui = fb.m_settingDefinition?.m_Definition;
                if (ui != null)
                    used.Add(ui.m_key);
            }

            foreach (var def in settings)
            {
                if (def != null && !used.Contains(def.m_key))
                {
                    EditorGUILayout.HelpBox(
                        $"SettingDefinition non utilisé dans l'UI : {def.m_key}",
                        MessageType.Info);
                }
            }

            // -------- SUMMARY ---------
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("---------------");
            EditorGUILayout.LabelField($"Erreurs : {errorCount}");
            EditorGUILayout.LabelField($"Warnings : {warnCount}");
            EditorGUILayout.LabelField("---------------");

            if (errorCount == 0)
            {
                EditorGUILayout.HelpBox("Audit terminé : AUCUNE ERREUR. Système propre et validé.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Des erreurs ont été détectées. Corrigez-les avant le build.", MessageType.Error);
            }
        }
    }
}
#endif

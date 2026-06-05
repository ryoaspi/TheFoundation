using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    public class ProjectNoteRegistry : MonoBehaviour
    {
        
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
        
        public static void MarkAllSeen(IEnumerable<ProjectNoteDefinition> projectNotes)
        {
            foreach (var pn in projectNotes)
                if (!Data.seen.Contains(pn.m_ID)) Data.seen.Add(pn.m_ID);
            EditorPrefs.SetString(KEY, JsonUtility.ToJson(Data));
        }

        public static void Reset()
        {
            EditorPrefs.DeleteKey(KEY);
            _data = null;
        }
        
        
        private const string KEY = "TheFoundation.ProjectNoteRegistry";
        private static RegistryData _data;

        [Serializable] 
        private class RegistryData { public List<string> seen = new(); }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheFoundation.Editor
{
    public static class ProjectNoteDatabase
    {
        public static List<ProjectNoteDefinition> LoadAll()
        {
            return AssetDatabase.FindAssets("t:ProjectNoteDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => File.GetCreationTime(path + ".meta"))
                .Select(AssetDatabase.LoadAssetAtPath<ProjectNoteDefinition>)
                .Where(pn => pn != null)
                .ToList();
        }
    }
}

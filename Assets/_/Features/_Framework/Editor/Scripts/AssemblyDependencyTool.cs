using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;


namespace TheFoundation.Editor
{ 
    
/// <summary>
/// Two tools in one file:
/// 1. DependencyOrderWindow   — shows assemblies sorted by reference count
/// 2. AssemblyHierarchyDrawer — draws colored dots in the Project window
///
/// Place this file anywhere inside an Editor-only folder (or an asmdef with Editor platform).
/// </summary>

// ─────────────────────────────────────────────────────────────
// DATA
// ─────────────────────────────────────────────────────────────
public static class AssemblyDependencyAnalyzer
{
    public class AssemblyInfo
    {
        public string Name;
        public string Path;
        public string FolderGuid;
        public List<string> References = new();
        public int Depth => References.Count;
    }

    // Palette: 0 refs → green, 1-2 → yellow, 3-4 → orange, 5+ → red
    public static Color GetColor(int depth)
    {
        if (depth == 0) return new Color(0.56f, 0.93f, 0.56f); // #90EE90
        if (depth <= 2) return new Color(1.00f, 0.84f, 0.00f); // #FFD700
        if (depth <= 4) return new Color(1.00f, 0.65f, 0.00f); // #FFA500
        return new Color(1.00f, 0.42f, 0.42f);                 // #FF6B6B
    }

    public static List<AssemblyInfo> Analyze(string rootFolder = "Assets/_/Features")
    {
        var result = new List<AssemblyInfo>();
        var guids = AssetDatabase.FindAssets("t:asmdef", new[] { rootFolder });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var json = File.ReadAllText(path);
            var info = new AssemblyInfo
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                Path = path,
                FolderGuid = AssetDatabase.AssetPathToGUID(
                    System.IO.Path.GetDirectoryName(path))
            };

            // Parse "references": [...] from the raw JSON (no dependencies on Newtonsoft)
            int refIdx = json.IndexOf("\"references\"");
            if (refIdx >= 0)
            {
                int open = json.IndexOf('[', refIdx);
                int close = json.IndexOf(']', open);
                if (open >= 0 && close > open)
                {
                    var block = json.Substring(open + 1, close - open - 1);
                    foreach (var part in block.Split(','))
                    {
                        var trimmed = part.Trim().Trim('"');
                        if (!string.IsNullOrEmpty(trimmed))
                            info.References.Add(trimmed);
                    }
                }
            }

            result.Add(info);
        }

        // Sort by reference count, then alphabetically
        result.Sort((a, b) =>
        {
            int cmp = a.Depth.CompareTo(b.Depth);
            return cmp != 0 ? cmp : string.Compare(a.Name, b.Name);
        });

        return result;
    }

    // Build a quick lookup: folderGuid → AssemblyInfo
    public static Dictionary<string, AssemblyInfo> BuildFolderMap(List<AssemblyInfo> infos)
    {
        var map = new Dictionary<string, AssemblyInfo>();
        foreach (var info in infos)
            if (!string.IsNullOrEmpty(info.FolderGuid))
                map[info.FolderGuid] = info;
        return map;
    }
}

// ─────────────────────────────────────────────────────────────
// 1. EDITOR WINDOW
// ─────────────────────────────────────────────────────────────
public class DependencyOrderWindow : EditorWindow
{
    private List<AssemblyDependencyAnalyzer.AssemblyInfo> _infos;
    private Vector2 _scroll;
    private string _rootFolder = "Assets/_/Features";

    [MenuItem("TheFoundation/Tools/Assembly Dependency Order")]
    public static void Open()
    {
        var w = GetWindow<DependencyOrderWindow>("Dependency Order");
        w.minSize = new Vector2(420, 300);
        w.Refresh();
    }

    private void Refresh() => _infos = AssemblyDependencyAnalyzer.Analyze(_rootFolder);

    private void OnGUI()
    {
        // ── toolbar ──
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Root folder:", GUILayout.Width(80));
        _rootFolder = EditorGUILayout.TextField(_rootFolder);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            Refresh();
        EditorGUILayout.EndHorizontal();

        if (_infos == null || _infos.Count == 0)
        {
            EditorGUILayout.HelpBox("No .asmdef found in the specified folder.", MessageType.Info);
            return;
        }

        // ── legend ──
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        DrawLegendEntry("0 refs",  AssemblyDependencyAnalyzer.GetColor(0));
        DrawLegendEntry("1-2 refs", AssemblyDependencyAnalyzer.GetColor(1));
        DrawLegendEntry("3-4 refs", AssemblyDependencyAnalyzer.GetColor(3));
        DrawLegendEntry("5+ refs",  AssemblyDependencyAnalyzer.GetColor(5));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        // ── list ──
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _infos.Count; i++)
        {
            var info = _infos[i];
            var color = AssemblyDependencyAnalyzer.GetColor(info.Depth);

            EditorGUILayout.BeginHorizontal();

            // Colored dot
            var dotRect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
            dotRect.y += 3;
            DrawDot(dotRect, color);

            // Index + name
            EditorGUILayout.LabelField($"{i:00}  {info.Name}", GUILayout.ExpandWidth(true));

            // Ref count badge
            var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = color }
            };
            EditorGUILayout.LabelField($"{info.Depth} ref{(info.Depth != 1 ? "s" : "")}", badgeStyle, GUILayout.Width(55));

            EditorGUILayout.EndHorizontal();

            // Collapsible references (click to expand — simple toggle via foldout)
            if (info.References.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var r in info.References)
                    EditorGUILayout.LabelField($"→ {r}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawLegendEntry(string label, Color color)
    {
        var r = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12), GUILayout.Height(12));
        r.y += 3;
        DrawDot(r, color);
        GUILayout.Space(2);
        EditorGUILayout.LabelField(label, GUILayout.Width(55));
    }

    private static void DrawDot(Rect rect, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
        GUI.color = prev;
    }
}

// ─────────────────────────────────────────────────────────────
// 2. PROJECT WINDOW OVERLAY (colored dots beside folder names)
// ─────────────────────────────────────────────────────────────
[InitializeOnLoad]
public static class AssemblyHierarchyDrawer
{
    private static Dictionary<string, AssemblyDependencyAnalyzer.AssemblyInfo> _folderMap;

    static AssemblyHierarchyDrawer()
    {
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItem;
        RefreshMap();
    }

    private static void RefreshMap()
    {
        var infos = AssemblyDependencyAnalyzer.Analyze("Assets/_/Features");
        _folderMap = AssemblyDependencyAnalyzer.BuildFolderMap(infos);
    }

    private static void OnProjectWindowItem(string guid, Rect selectionRect)
    {
        if (_folderMap == null) return;
        if (!_folderMap.TryGetValue(guid, out var info)) return;

        // Draw a small colored square on the right side of the row
        const float size = 8f;
        var dotRect = new Rect(
            selectionRect.xMax - size - 2f,
            selectionRect.y + (selectionRect.height - size) * 0.5f,
            size, size);

        var prev = GUI.color;
        GUI.color = AssemblyDependencyAnalyzer.GetColor(info.Depth);
        GUI.DrawTexture(dotRect, EditorGUIUtility.whiteTexture);
        GUI.color = prev;
    }
}
}

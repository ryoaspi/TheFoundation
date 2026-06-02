#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheFundation.Runtime;

public class LocalizationEditorWindow : EditorWindow
{
    private string _languageCode = "en";
    private LocalizationData _data;
    private Vector2 _scroll;

    private string _newKey;
    private string _newValue;

    private const string LocalizationFolder = "Assets/_/Resources/Localization";
    private const string ReferenceLanguage = "en";
    private const string MissingTranslationPlaceholder = "TODO_TRANSLATE";
    
    private Dictionary<string, string> _referenceDict = new Dictionary<string, string>();
    private LocalizationData _referenceData;
    private string _referenceJsonCache;
    
    private string _searchKey;
    private Dictionary<string, string> _keyValuesByLanguage = new Dictionary<string, string>();
    private List<string> _availableLanguages = new List<string>();

    [MenuItem("TheFundation/Localization Editor")]
    public static void Open()
    {
        GetWindow<LocalizationEditorWindow>("Localization Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Localization File", EditorStyles.boldLabel);

        _languageCode = EditorGUILayout.TextField("Language Code", _languageCode);

        if (GUILayout.Button("Load / Create"))
        {
            _LoadOrCreateFile();
            _LoadReferenceCache();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Sync missing keys from EN"))
        {
            _SyncFromReferenceLanguage();
        }

        if (_data == null)
            return;
        
        GUILayout.Space(10);
        GUILayout.Label("Key Inspector (All Languages)", EditorStyles.boldLabel);

        if (GUILayout.Button("Reload Languages List"))
        {
            _ReloadAvailableLanguages();
        }

        _searchKey = EditorGUILayout.TextField("Search Key", _searchKey);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Search"))
        {
            _SearchKeyInAllLanguages();
        }
        if (GUILayout.Button("Save Key (All Languages)"))
        {
            _SaveSearchedKeyToAllLanguages();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(_searchKey) && _availableLanguages.Count > 0)
        {
            foreach (var lang in _availableLanguages)
            {
                _keyValuesByLanguage.TryGetValue(lang, out var val);
                _keyValuesByLanguage[lang] = EditorGUILayout.TextField(lang.ToUpperInvariant(), val);
            }
        }


        GUILayout.Space(10);
        GUILayout.Label("Existing Keys", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var item in _data.items)
        {
            bool isUntranslated = _IsUntranslated(item);

            if (isUntranslated)
                GUI.color = Color.yellow;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.key, GUILayout.Width(250));
            item.value = EditorGUILayout.TextField(item.value);
            EditorGUILayout.EndHorizontal();

            GUI.color = Color.white;
        }


        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Save All"))
        {
            _Save();
        }

        GUILayout.Space(10);
        GUILayout.Label("Add New Key", EditorStyles.boldLabel);

        _newKey = EditorGUILayout.TextField("Key", _newKey);
        _newValue = EditorGUILayout.TextField("Value", _newValue);

        if (GUILayout.Button("Add Key"))
        {
            _AddKey();
        }
    }

    #region Main Methods

    private void _LoadOrCreateFile()
    {
        if (!Directory.Exists(LocalizationFolder))
            Directory.CreateDirectory(LocalizationFolder);

        string path = $"{LocalizationFolder}/{_languageCode}.json";

        if (!File.Exists(path))
        {
            _data = new LocalizationData { items = new List<LocalizationItem>() };
            _Save();
            
            if (_data.items == null)
                _data.items = new List<LocalizationItem>();
        }
        
        else
        {
            var json = File.ReadAllText(path);
            _data = JsonUtility.FromJson<LocalizationData>(json);
            
            if (_data ==  null)
                _data = new LocalizationData { items = new List<LocalizationItem>() };
            
            if (_data.items == null)
                _data.items = new List<LocalizationItem>();
        }
    }

    private void _Save()
    {
        string path = $"{LocalizationFolder}/{_languageCode}.json";
        File.WriteAllText(path, JsonUtility.ToJson(_data, true), Encoding.UTF8);
        AssetDatabase.Refresh();
    }

    private void _AddKey()
    {
        if (string.IsNullOrWhiteSpace(_newKey))
            return;

        if (_data.items.Exists(i => i.key == _newKey))
        {
            Debug.LogWarning($"Key '{_newKey}' already exists.");
            return;
        }

        _data.items.Add(new LocalizationItem
        {
            key = _newKey,
            value = _newValue
        });

        _newKey = "";
        _newValue = "";
    }
    
    private void _SyncFromReferenceLanguage()
    {
        var referencePath = $"{LocalizationFolder}/{ReferenceLanguage}.json";
        var targetPath = $"{LocalizationFolder}/{_languageCode}.json";

        if (!File.Exists(referencePath) || !File.Exists(targetPath))
        {
            Debug.LogWarning("Reference or target localization file missing.");
            return;
        }

        var refData = JsonUtility.FromJson<LocalizationData>(
            File.ReadAllText(referencePath)
        );
        
        if (refData == null) refData = new LocalizationData { items = new List<LocalizationItem>() };
        if (refData.items == null) refData.items = new List<LocalizationItem>();

        var targetData = JsonUtility.FromJson<LocalizationData>(
            File.ReadAllText(targetPath)
        );
        
        if (targetData == null) targetData = new LocalizationData { items = new List<LocalizationItem>() };
        if (targetData.items == null) targetData.items = new List<LocalizationItem>();

        int addedCount = 0;

        foreach (var refItem in refData.items)
        {
            if (!targetData.items.Exists(i => i.key == refItem.key))
            {
                targetData.items.Add(new LocalizationItem
                {
                    key = refItem.key,
                    value = MissingTranslationPlaceholder
                });
                addedCount++;
            }
        }

        File.WriteAllText(
            targetPath,
            JsonUtility.ToJson(targetData, true)
        );

        AssetDatabase.Refresh();

        Debug.Log($"[Localization] Sync complete. Added {addedCount} missing keys.");
        _data = targetData;
        _LoadReferenceCache();
    }

    private bool _IsUntranslated(LocalizationItem item)
    {
        if (_languageCode == ReferenceLanguage)
            return false;

        if (item == null)
            return false;

        if (string.IsNullOrWhiteSpace(item.value))
            return true;

        if (item.value.Contains(MissingTranslationPlaceholder))
            return true;

        if (_referenceDict == null || _referenceDict.Count == 0)
            return false;

        if (!_referenceDict.TryGetValue(item.key, out var refValue))
            return false;

        return string.Equals(refValue, item.value, StringComparison.Ordinal);
    }

    
    private void _LoadReferenceCache()
    {
        _referenceDict.Clear();
        _referenceData = null;
        _referenceJsonCache = null;

        var refPath = $"{LocalizationFolder}/{ReferenceLanguage}.json";
        if (!File.Exists(refPath))
            return;

        _referenceJsonCache = File.ReadAllText(refPath);
        _referenceData = JsonUtility.FromJson<LocalizationData>(_referenceJsonCache);

        if (_referenceData == null)
            _referenceData = new LocalizationData { items = new List<LocalizationItem>() };

        if (_referenceData.items == null)
            _referenceData.items = new List<LocalizationItem>();

        foreach (var it in _referenceData.items)
        {
            if (string.IsNullOrWhiteSpace(it.key))
                continue;

            _referenceDict[it.key] = it.value ?? string.Empty;
        }
    }

    private void _ReloadAvailableLanguages()
    {
        _availableLanguages.Clear();

        if (!Directory.Exists(LocalizationFolder))
            return;

        var files = Directory.GetFiles(LocalizationFolder, "*.json", SearchOption.AllDirectories);
        foreach (var f in files)
        {
            var lang = Path.GetFileNameWithoutExtension(f);
            if (!string.IsNullOrWhiteSpace(lang))
                _availableLanguages.Add(lang);
        }

        _availableLanguages.Sort();
    }
    
    private void _SearchKeyInAllLanguages()
    {
        _keyValuesByLanguage.Clear();
        _ReloadAvailableLanguages();

        if (string.IsNullOrWhiteSpace(_searchKey))
            return;

        foreach (var lang in _availableLanguages)
        {
            var path = $"{LocalizationFolder}/{lang}.json";
            if (!File.Exists(path))
                continue;

            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<LocalizationData>(json);

            if (data == null) data = new LocalizationData { items = new List<LocalizationItem>() };
            if (data.items == null) data.items = new List<LocalizationItem>();

            var item = data.items.Find(i => i.key == _searchKey);
            _keyValuesByLanguage[lang] = item != null ? (item.value ?? string.Empty) : string.Empty;
        }
    }

    private void _SaveSearchedKeyToAllLanguages()
    {
        if (string.IsNullOrWhiteSpace(_searchKey))
            return;

        _ReloadAvailableLanguages();

        foreach (var lang in _availableLanguages)
        {
            var path = $"{LocalizationFolder}/{lang}.json";
            if (!File.Exists(path))
                continue;

            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<LocalizationData>(json);

            if (data == null) data = new LocalizationData { items = new List<LocalizationItem>() };
            if (data.items == null) data.items = new List<LocalizationItem>();

            var item = data.items.Find(i => i.key == _searchKey);

            var newValue = _keyValuesByLanguage.TryGetValue(lang, out var v) ? v : string.Empty;

            if (item == null)
            {
                data.items.Add(new LocalizationItem { key = _searchKey, value = newValue });
            }
            else
            {
                item.value = newValue;
            }

            File.WriteAllText(path, JsonUtility.ToJson(data, true), Encoding.UTF8);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Localization] Saved key '{_searchKey}' across all languages.");
    }

    
    #endregion
}
#endif

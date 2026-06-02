using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace TheFundation.Runtime
{
    [Serializable]
    public class SerializableFact
    {
        public string key;
        public string TypeName;
        public string JsonValue;
        public bool IsPersistent;
    }

    [Serializable]
    public class SerializationWrapper
    {
        public int SaveVersion = 1;
        public string BuildId;
        public List<SerializableFact> Facts;
    }

    [Serializable]
    public class PrimitiveWrapper<T>
    {
        public T value;
        public PrimitiveWrapper(T v) { value = v; }
    }

    public static class FactSaveSystem
    {
        #region Publics

        public const string m_buildId = "BETA_0_1_0";

        public static string GetSaveDirectory()
        {
#if UNITY_EDITOR
            // En Editor : toujours sandbox Unity (évite Program Files)
            return Path.Combine(Application.persistentDataPath, "Save");

#elif UNITY_STANDALONE
            // En build PC : dossier du jeu
            return Path.Combine(AppContext.BaseDirectory, "Save");

#else
            // Mobile / autres plateformes
            return Path.Combine(Application.persistentDataPath, "Save");
#endif
        }

        public static string GetMainSavePath()
        {
            return Path.Combine(GetSaveDirectory(), "facts_save.json.gz");
        }

        public static string GetSlotPath(int slot)
        {
            return Path.Combine(GetSaveDirectory(), $"slot_{slot}.json.gz");
        }

        public static bool SlotExist(int slot)
        {
            return File.Exists(GetSlotPath(slot));
        }

        public static void DeleteSlot(int slot)
        {
            var path = GetSlotPath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void DeleteAllSaves()
        {
            var dir = GetSaveDirectory();
            if (!Directory.Exists(dir))
            {
                return;
            }
                Directory.Delete(dir, true);
        }

        #endregion
        

        #region Utils

        public static void SaveToFile(FactDictionary m_dictionary)
        {
            WriteGz(GetMainSavePath(), SaveToJson(m_dictionary));
        }

        public static void LoadFromFile(FactDictionary m_dictionary)
        {
            var path = GetMainSavePath();
            if (!File.Exists(path)) return;

            LoadFromJson(m_dictionary, ReadGz(path));
        }

        public static void SaveToSlot(FactDictionary m_dictionary, int m_slot)
        {
            WriteGz(GetSlotPath(m_slot), SaveToJson(m_dictionary));
        }

        public static void LoadFromSlot(FactDictionary m_dictionary, int m_slot)
        {
            var path = GetSlotPath(m_slot);
            if (!File.Exists(path)) return;

            LoadFromJson(m_dictionary, ReadGz(path));
        }

        #endregion

        #region Main Methods

        static void EnsureSaveDirectory()
        {
            var dir = GetSaveDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        static string SaveToJson(FactDictionary m_dictionary)
        {
            var list = new List<SerializableFact>();

            foreach (var kv in m_dictionary.m_allFacts)
            {
                if (!kv.Value.IsSaved) continue;

                var value = kv.Value.GetObjectValue();
                var type = kv.Value.valueType;

                string json =
                    (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                        ? JsonUtility.ToJson(
                            Activator.CreateInstance(
                                typeof(PrimitiveWrapper<>).MakeGenericType(type), value))
                        : JsonUtility.ToJson(value);

                list.Add(new SerializableFact
                {
                    key = kv.Key,
                    TypeName = type.AssemblyQualifiedName,
                    JsonValue = json,
                    IsPersistent = kv.Value.IsPersistent
                });
            }

            return JsonUtility.ToJson(new SerializationWrapper
            {
                SaveVersion = 1,
                BuildId = m_buildId,
                Facts = list
            });
        }

        static void LoadFromJson(FactDictionary m_dictionary, string m_json)
        {
            if (string.IsNullOrEmpty(m_json)) return;

            var wrapper = JsonUtility.FromJson<SerializationWrapper>(m_json);
            if (wrapper == null || wrapper.Facts == null) return;

            if (wrapper.BuildId != m_buildId)
            {
                Debug.LogWarning("Save ignorée : BuildId incompatible.");
                return;
            }

            foreach (var s in wrapper.Facts)
            {
                if (string.IsNullOrEmpty(s.key)) continue;

                var type = ResolveType(s.TypeName);
                if (type == null) continue;

                object value =
                    (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                        ? GetWrappedValue(type, s.JsonValue)
                        : JsonUtility.FromJson(s.JsonValue, type);

                var persistence = s.IsPersistent
                    ? FactDictionary.FactPersistence.Persistent
                    : FactDictionary.FactPersistence.Normal;

                typeof(FactDictionary)
                    .GetMethod(nameof(FactDictionary.SetFact))!
                    .MakeGenericMethod(type)
                    .Invoke(m_dictionary, new object[] { s.key, value, persistence, true });
            }
        }

        static void WriteGz(string m_path, string m_json)
        {
            EnsureSaveDirectory();

            using var fs = File.Create(m_path);
            using var gz = new GZipStream(fs, CompressionLevel.Optimal);
            using var sw = new StreamWriter(gz);
            sw.Write(m_json);
        }

        static string ReadGz(string m_path)
        {
            using var fs = File.OpenRead(m_path);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var sr = new StreamReader(gz);
            return sr.ReadToEnd();
        }

        #endregion

        #region Private and Protected

        static Type ResolveType(string m_assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(m_assemblyQualifiedName)) return null;

            var type = Type.GetType(m_assemblyQualifiedName);
            if (type != null) return type;

            var fullName = m_assemblyQualifiedName.Split(',')[0].Trim();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }

        static object GetWrappedValue(Type m_type, string m_json)
        {
            var wrapperType = typeof(PrimitiveWrapper<>).MakeGenericType(m_type);
            var instance = JsonUtility.FromJson(m_json, wrapperType);
            return wrapperType.GetField("value")!.GetValue(instance);
        }

        #endregion
    }
}

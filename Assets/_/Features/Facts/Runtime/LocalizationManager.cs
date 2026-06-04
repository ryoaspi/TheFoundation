using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheFoundation.Runtime
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        public static string CurrentLanguage { get; private set; } = "en";
        public static event Action OnLanguageChanged;

        static Dictionary<string, string> _texts = new();

        void Awake()
        {
            transform.SetParent(null);
            
            if (Instance) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            bool userChosen = GameManager.Facts.TryGetFact<int>("language.userChosen", out var chosen) && chosen == 1;

            if (userChosen && GameManager.Facts.TryGetFact<string>("language", out var savedLang))
            {
                LoadLanguage(savedLang, save: false);
                return;
            }

            // Si pas choisi par l'utilisateur : on tente quand même d'utiliser language si existant
            if (GameManager.Facts.TryGetFact<string>("language", out var lang))
            {
                LoadLanguage(lang, save: false);
                return;
            }

            // Sinon on détecte l'OS et on sauvegarde comme "auto"
            string detected = DetectSystemLanguage();
            LoadLanguage(detected, save: true);

            // Et on marque "pas choisi par l'utilisateur"
            GameManager.Facts.SetFact("language.userChosen", 0, FactDictionary.FactPersistence.Persistent);
        }

        public static void SetLanguage(string lang)
        {
            LoadLanguage(lang, save: true);
            GameManager.Facts.SetFact("language.userChosen", 1, FactDictionary.FactPersistence.Persistent);
            FactSaveSystem.SaveToFile(GameManager.Facts);
        }

        public static void LoadLanguage(string language, bool save)
        {
            CurrentLanguage = language;
            _texts.Clear();

            var file = Resources.Load<TextAsset>($"Localization/{language}") ??
                       Resources.Load<TextAsset>("Localization/en");

            if (!file)
            {
                Debug.LogWarning($"Localization file not found for '{language}' nor fallback 'en'.");
                return;
            }

            var data = JsonUtility.FromJson<LocalizationData>(file.text);
            foreach (var it in data.items)
                _texts[it.key] = it.value;

            if (save)
                GameManager.Facts.SetFact("language", language, FactDictionary.FactPersistence.Persistent);
            
            OnLanguageChanged?.Invoke();
        }

        public static string GetText(string key)
            => _texts.TryGetValue(key, out var v) ? v : $"[{key}]";
        

        private static string DetectSystemLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.English:
                    return "en";
                default:
                    return "en";
            }
        }
    }

    [Serializable] public class LocalizationData { public List<LocalizationItem> items; }
    [Serializable] public class LocalizationItem { public string key; public string value; }
}
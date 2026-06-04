using System;
using TheFoundation.Runtime.Events;
using UnityEngine;

namespace TheFoundation.Runtime
{
    /// <summary>
    /// GameManager — version étendue
    /// --------------------------------
    /// Intègre les trois nouveaux services :
    ///   - EventBus    (nettoyage au quit)
    ///   - SceneService (initialisation)
    ///   - AudioService (initialisation)
    ///
    /// Ordre d'init garanti :
    ///   1. Load facts
    ///   2. Declare facts
    ///   3. Language
    ///   4. Platform
    ///   5. Settings      ← requis avant AudioService (volumes)
    ///   6. Audio         ← nouveau
    ///   7. Goals
    ///   8. Version
    ///   9. Scene         ← nouveau (après tout le reste)
    ///  10. HasSave
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager m_Instance { get; private set; }
        public static FactDictionary Facts { get; } = new();
        public const int _MaxSlots = 10;

        public VersionDefinition m_versionDefinition;
        public static bool IsGameLoaded { get; private set; }

        void Awake()
        {
            if (m_Instance) { Destroy(gameObject); return; }
            m_Instance = this;
            DontDestroyOnLoad(gameObject);

            // 1 ■ Load
            FactSaveSystem.LoadFromFile(Facts);

            // 2 ■ Declare
            DeclareGameFacts();
            IsGameLoaded = true;

            // 3 ■ Language
            if (Facts.TryGetFact("language", out string lang))
                LocalizationManager.SetLanguage(lang);
            else
                LocalizationManager.SetLanguage("en");

            // 4 ■ Platform
            PlatformizerService.Initialize();

            // 5 ■ Settings (doit précéder AudioService)
            SettingsService.Initialize(_settingsDefinitions);

            // 6 ■ Audio
            AudioService.Initialize(_audioCollection);

            // 7 ■ Goals
            GoalsService.Initialize(_Goals);

            // 8 ■ Version
            VersionService.m_definition = m_versionDefinition;
            VersionService.Initialize();

            // 9 ■ Scene
            SceneService.Initialize();

            // 10 ■ Has save
            RefreshHasSaveFact();
        }

        private void Update()
        {
            _saveTimer += Time.deltaTime;
            if (_saveTimer >= _autoSaveInterval)
            {
                _saveTimer = 0;
                AutoSave();
            }
        }

        void OnApplicationPause(bool p)
        {
            if (p) FactSaveSystem.SaveToFile(Facts);
        }

        void OnApplicationQuit()
        {
            FactSaveSystem.SaveToFile(Facts);
            EventBus.ClearAll(); // nettoyage propre
        }

        // ─── API ────────────────────────────────────────────────

        public static void SaveToSlot(int slot)
        {
            FactSaveSystem.SaveToSlot(Facts, slot);
            EventBus.Raise(new OnGameSaved { slot = slot });
        }

        public static void LoadFromSlot(int slot)
        {
            FactSaveSystem.LoadFromSlot(Facts, slot);
            EventBus.Raise(new OnGameLoaded { slot = slot });
        }

        public static void DeleteSlot(int slot)   => FactSaveSystem.DeleteSlot(slot);
        public static bool HasSaveInSlot(int slot) => FactSaveSystem.SlotExist(slot);

        public static bool AnySaveExists()
        {
            for (int i = 0; i < _MaxSlots; i++)
                if (HasSaveInSlot(i)) return true;
            return false;
        }

        public void RefreshHasSaveFact()
        {
            Facts.SetFact("has_save", AnySaveExists(), FactDictionary.FactPersistence.Normal);
        }

        // ─── Privés ─────────────────────────────────────────────

        private void DeclareGameFacts()
        {
            foreach (var mono in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (mono is IGameFactsProvider provider)
                    provider.DeclareFacts(Facts);
            }
        }

        private void AutoSave()
        {
            FactSaveSystem.SaveToFile(Facts);
        }

        // ─── Serialized fields ──────────────────────────────────

        [SerializeField] private SettingsDefinitionCollection _settingsDefinitions;
        [SerializeField] private AudioCollection _audioCollection;   // nouveau
        [SerializeField] private GoalsCollection _Goals;
        [SerializeField] private float _autoSaveInterval = 5f;
        private float _saveTimer = 0f;
    }
}
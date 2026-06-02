using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// SettingsService
    /// ---------------
    /// Service global (non-MonoBehaviour) qui :
    /// - lit la définition des paramètres depuis un SettingsDefinitionCollection (ScriptableObject),
    /// - initialise les valeurs dans GameManager.Facts si elles n’existent pas,
    /// - expose une API simple (Get/Set Float, Bool, Dropdown),
    /// - applique certains effets (ex : volume global).
    ///
    /// Les valeurs sont stockées dans FactDictionary (V1) avec FactPersistence.Persistent.
    /// </summary>
    public static class SettingsService
    {
        #region Publics

        /// <summary>
        /// Indique si le service a déjà été initialisé.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        #endregion



        #region Initialization

        private static SettingsDefinitionCollection _definitions;

        /// <summary>
        /// Initialise le système de réglages :
        /// - garde une référence vers la collection de définitions,
        /// - crée les facts manquants avec leurs valeurs par défaut,
        /// - applique tous les réglages.
        /// 
        /// À appeler une seule fois au démarrage (GameManager, bootstrap, etc.).
        /// </summary>
        public static void Initialize(SettingsDefinitionCollection definitions)
        {
            if (IsInitialized) return;
            if (definitions == null)
            {
                Debug.LogError("[SettingsService] Initialize appelé avec une collection nulle.");
                return;
            }

            _definitions = definitions;

            EnsureDefaultsInFacts();
            ApplyAll();

            IsInitialized = true;

#if UNITY_EDITOR
            Debug.Log("[SettingsService] Initialisé.");
#endif
        }

        #endregion



        #region API - Getters / Setters

        /// <summary>
        /// Lit un float (volume, sensibilité, etc.).
        /// Si le fact n’existe pas, renvoie la valeur par défaut définie dans SettingDefinition.
        /// </summary>
        public static float GetFloat(string key)
        {
            var def = GetDefinition(key);
            if (def == null) return 0f;

            float defaultValue = def.m_defaultFloat;

            if (GameManager.Facts.TryGetFact(key, out float value))
                return value;

            return defaultValue;
        }

        /// <summary>
        /// Modifie un float et applique le réglage correspondant.
        /// </summary>
        public static void SetFloat(string key, float value)
        {
            var def = GetDefinition(key);
            if (def == null) return;

            // Clamp selon min / max de la définition
            value = Mathf.Clamp(value, def.m_minFloat, def.m_maxFloat);

            GameManager.Facts.SetFact(key, value, FactDictionary.FactPersistence.Persistent);
            Apply(key);
        }

        /// <summary>
        /// Lit un bool (ex : vibrations, aim assist).
        /// </summary>
        public static bool GetBool(string key)
        {
            var def = GetDefinition(key);
            if (def == null) return false;

            bool defaultValue = def.m_defaultBool;

            if (GameManager.Facts.TryGetFact(key, out bool value))
                return value;

            return defaultValue;
        }

        /// <summary>
        /// Modifie un bool et applique le réglage correspondant.
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            var def = GetDefinition(key);
            if (def == null) return;

            GameManager.Facts.SetFact(key, value, FactDictionary.FactPersistence.Persistent);
            Apply(key);
        }

        /// <summary>
        /// Lit l’index sélectionné pour un réglage de type Dropdown.
        /// </summary>
        public static int GetDropdownIndex(string key)
        {
            var def = GetDefinition(key);
            if (def == null) return 0;

            if (GameManager.Facts.TryGetFact(key, out int value))
                return value;

            // Par défaut : premier élément
            return 0;
        }

        /// <summary>
        /// Modifie l’index d’un Dropdown et applique le réglage correspondant.
        /// </summary>
        public static void SetDropdownIndex(string key, int value)
        {
            var def = GetDefinition(key);
            if (def == null) return;

            // Clamp sur la taille des options
            int maxIndex = (def.m_options != null && def.m_options.Length > 0)
                ? def.m_options.Length - 1
                : 0;

            value = Mathf.Clamp(value, 0, maxIndex);

            GameManager.Facts.SetFact(key, value, FactDictionary.FactPersistence.Persistent);
            Apply(key);
        }

        #endregion



        #region Application (ApplyAll / ApplyOne)

        /// <summary>
        /// Applique tous les réglages définis dans la collection.
        /// Appelé au démarrage après Initialize.
        /// </summary>
        public static void ApplyAll()
        {
            if (_definitions == null || _definitions.m_Settings == null) return;

            foreach (var def in _definitions.m_Settings)
            {
                if (def == null || string.IsNullOrEmpty(def.m_key))
                    continue;

                Apply(def.m_key);
            }
        }

        /// <summary>
        /// Applique le réglage correspondant à la clé donnée.
        /// Ne fait quelque chose que si ce réglage a un effet “runtime”
        /// (ex : volume, brightness, etc.).
        /// </summary>
        public static void Apply(string key)
        {
            var def = GetDefinition(key);
            if (def == null) return;

            switch (def.m_type)
            {
                case SettingType.Float:
                    float f = GetFloat(def.m_key);
                    ApplyFloat(def, f);
                    break;

                case SettingType.Bool:
                    bool b = GetBool(def.m_key);
                    ApplyBool(def, b);
                    break;

                case SettingType.Dropdown:
                    int idx = GetDropdownIndex(def.m_key);
                    ApplyDropdown(def, idx);
                    break;
            }
        }

        #endregion



        #region Internal Helpers

        private static SettingsDefinitionCollection Definitions => _definitions;

        /// <summary>
        /// Récupère la définition pour une clé donnée.
        /// </summary>
        private static SettingDefinition GetDefinition(string key)
        {
            if (Definitions == null || Definitions.m_Settings == null) return null;
            if (string.IsNullOrEmpty(key)) return null;

            foreach (var def in Definitions.m_Settings)
            {
                if (def != null && def.m_key == key)
                    return def;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[SettingsService] Aucune SettingDefinition trouvée pour la clé '{key}'.");
#endif
            return null;
        }

        /// <summary>
        /// Crée les facts manquants avec leurs valeurs par défaut.
        /// </summary>
        private static void EnsureDefaultsInFacts()
        {
            if (Definitions == null || Definitions.m_Settings == null) return;

            foreach (var def in Definitions.m_Settings)
            {
                if (def == null || string.IsNullOrEmpty(def.m_key))
                    continue;

                switch (def.m_type)
                {
                    case SettingType.Float:
                        if (!GameManager.Facts.TryGetFact(def.m_key, out float _))
                        {
                            GameManager.Facts.SetFact(
                                def.m_key,
                                Mathf.Clamp(def.m_defaultFloat, def.m_minFloat, def.m_maxFloat),
                                FactDictionary.FactPersistence.Persistent);
                        }
                        break;

                    case SettingType.Bool:
                        if (!GameManager.Facts.TryGetFact(def.m_key, out bool _))
                        {
                            GameManager.Facts.SetFact(
                                def.m_key,
                                def.m_defaultBool,
                                FactDictionary.FactPersistence.Persistent);
                        }
                        break;

                    case SettingType.Dropdown:
                        if (!GameManager.Facts.TryGetFact(def.m_key, out int _))
                        {
                            GameManager.Facts.SetFact(
                                def.m_key,
                                0,
                                FactDictionary.FactPersistence.Persistent);
                        }
                        break;
                }
            }
        }

        #endregion



        #region Application Details (Float / Bool / Dropdown)

        /// <summary>
        /// Applique un réglage float particulier selon sa définition.
        /// Ici, on ne gère que quelques cas "connus" (ex : volume global).
        /// Le reste est à étendre selon les besoins de tes jeux.
        /// </summary>
        private static void ApplyFloat(SettingDefinition def, float value)
        {
            // Exemple : volume global du jeu
            if (def.m_key == "settings_masterVolume")
            {
                AudioListener.volume = value;
            }

            // Exemple : futur brightness, sensibilité, etc.
            // if (def.m_Key == "settings_brightness") { ... }
            // if (def.m_Key == "settings_sensitivity") { ... }
        }

        /// <summary>
        /// Applique un réglage bool (ex : vibrations, camera shake…).
        /// </summary>
        private static void ApplyBool(SettingDefinition def, bool value)
        {
            // Exemple : activer / désactiver un système de vibration custom
            // if (def.m_Key == "settings_vibration") { MyVibrationSystem.Enabled = value; }
        }

        /// <summary>
        /// Applique un Dropdown (ex : qualité, résolution, langue…).
        /// </summary>
        private static void ApplyDropdown(SettingDefinition def, int index)
        {
            // Exemple : langue
            // if (def.m_Key == "settings_language")
            // {
            //     string langCode = def.m_Options[index];
            //     LocalizationManager.SetLanguage(langCode);
            // }
        }

        #endregion
    }
}

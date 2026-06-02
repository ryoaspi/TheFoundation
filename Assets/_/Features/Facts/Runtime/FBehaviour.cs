using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheFundation.Runtime
{
    public class FBehaviour : MonoBehaviour
    {
        #region Publics (Services & UI Definitions)

        /// <summary>
        /// Définition UI optionnelle pour un réglage
        /// (slider / toggle / dropdown).
        /// 
        /// Si renseignée dans l’inspector, FBehaviour
        /// tentera automatiquement de binder le composant UI.
        /// </summary>
        [Serializable]
        public class SettingUIDefinitionData
        {
            public SettingDefinition m_Definition;
        }
        
        [Header("Settings UI (optionnel)")]
        public SettingUIDefinitionData m_settingDefinition;

        /// <summary>
        /// Accès direct au système de Facts V1.
        /// </summary>
        public FactDictionary Facts => GameManager.Facts;
        
        public bool m_debug { get => _debug; set => _debug = value; }
        public bool m_warning { get => _warning; set => _warning = value; }
        public bool m_error { get => _error; set => _error = value; }

        #endregion
        

        #region API Unity
        
        protected virtual void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        protected virtual void Start()
        {
            // Auto-binder des réglages UI
            AutoBindSettingIfNeeded();
        }
        
        protected virtual void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }
        
        #endregion
        

        #region Utils (Méthodes publiques)

        public T GetFact<T>(string key, T defaultValue = default)
        {
            return Facts.TryGetFact(key, out T v) ? v : defaultValue;
        }

        public void SetFact<T>(string key, T value,
            FactDictionary.FactPersistence persistence = FactDictionary.FactPersistence.Normal)
        {
            Facts.SetFact(key, value, persistence);
        }

        /// <summary>
        /// Sauvegarde immédiate des Facts.
        /// ⚠ À utiliser uniquement pour UI, debug ou actions volontaires.
        /// Le cycle automatique est géré par GameManager.
        /// </summary>
        public void SaveFacts() => FactSaveSystem.SaveToFile(Facts);
        public void LoadFacts() => FactSaveSystem.LoadFromFile(Facts);

        public void DebugFact(string key)
        {
            if (Facts.TryGetFact<object>(key, out object v))
                Debug.Log($"[Fact] {key} = {v}");
            else
                Debug.Log($"[Fact] {key} n'existe pas.");
        }

        #endregion

        
        #region Main Methods (Méthodes privées)

        private void AutoBindSettingIfNeeded()
        {
            var def = m_settingDefinition?.m_Definition;
            if (def == null)
                return;

            // Bind Slider
            if (TryGetComponent<Slider>(out var slider))
            {
                SliderBinderLogic.Bind(slider, def);
                ApplyPlatformVisibility(def);
                // ApplyLocalization(def);
                return;
            }

            // Bind Toggle
            if (TryGetComponent<Toggle>(out var toggle))
            {
                ToggleBinderLogic.Bind(toggle, def);
                ApplyPlatformVisibility(def);
                // ApplyLocalization(def);
                return;
            }

            // Bind Dropdown
            if (TryGetComponent<Dropdown>(out var dropdown))
            {
                DropdownBinderLogic.Bind(dropdown, def);
                ApplyPlatformVisibility(def);
                // ApplyLocalization(def);
                return;
            }
        }

        /// <summary>
        /// Masque l'UI selon la plateforme (Desktop / Mobile / Console).
        /// </summary>
        private void ApplyPlatformVisibility(SettingDefinition def)
        {
            string family = GetFact("platform_family", "Desktop");

            bool show =
                (family == "Desktop" && def.m_showOnDesktop) ||
                (family == "Mobile" && def.m_showOnMobile) ||
                (family == "Console" && def.m_showOnConsole);

            gameObject.SetActive(show);
        }

        /// <summary>
        /// Applique la localisation sur le label et la description
        /// (si présents dans l’inspector).
        /// </summary>
        // private void ApplyLocalization(SettingDefinition def)
        // {
        //     if (m_label)
        //     {
        //         if (!string.IsNullOrEmpty(def.m_labelKey))
        //             m_label.text = LocalizationManager.GetText(def.m_labelKey);
        //         else
        //             m_label.text = def.m_label;
        //     }
        //
        //     if (m_description)
        //     {
        //         if (!string.IsNullOrEmpty(def.m_descriptionKey))
        //             m_description.text = LocalizationManager.GetText(def.m_descriptionKey);
        //         else
        //             m_description.text = def.m_description;
        //     }
        //     
        // }

        protected void GoalNotify(string key, int amount = 1)
        {
            GoalsService.Notify(key, amount);
        }

        private void HandleLanguageChanged()
        {
            OnLocalizationChanged();
        }
        
        /// <summary>
        /// Hook appelé automatiquement quand la langue change.
        /// Les classes dérivées peuvent override pour rafraîchir leurs textes.
        /// </summary>
        protected virtual void OnLocalizationChanged()
        {
            // --- AJOUT POUR LE DROPDOWN ET LES SETTINGS ---
            var def = m_settingDefinition?.m_Definition;
            if (def != null)
            {
                // On relance la traduction du Label et de la Description
                // ApplyLocalization(def);

                // Si c'est un Dropdown TMP, on force le rafraîchissement visuel
                if (TryGetComponent<TMP_Dropdown>(out var tmpDropdown))
                {
                    tmpDropdown.RefreshShownValue();
                }
                // Si c'est un Dropdown classique Unity
                else if (TryGetComponent<Dropdown>(out var dropdown))
                {
                    dropdown.RefreshShownValue();
                }
            }
        }
        
        protected string L(string key, string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            var value = LocalizationManager.GetText(key);
            // Si la key est manquante, ton manager renvoie "[key]".
            if (value == $"[{key}]")
                return fallback ?? value;

            return value;
        }

        protected void SetLocalizedText(TMP_Text target, string key, string fallback = null)
        {
            if (!target)
                return;

            target.text = L(key, fallback);
        }

        #endregion
        
        
        #region Facts Declaration (Framework Core)

        /// <summary>
        /// Permet à un jeu ou système de déclarer ses facts.
        /// Appelé UNE SEULE FOIS au démarrage par le GameManager.
        /// </summary>
        protected virtual void DeclareFacts(FactDictionary facts)
        {
            // Intentionnellement vide
            // Chaque jeu override cette méthode
        }

        internal void InternalDeclareFacts()
        {
            DeclareFacts(GameManager.Facts);
        }

        #endregion
        
        
        #region Debug
        
        protected void Info(string message, Object context = null)
        {
            if (!_debug) return;
            Debug.Log($"<color=cyan> FROM: {this} | INFO: {message} </color>", context);
        }
   
        protected void InfoInProgress(string message, Object context = null)
        {
            if (!_debug) return;
            Debug.Log($"<color=orange> FROM: {this} | IN_PROGRESS: {message} </color>", context);
        }
        
        protected void InfoDone(string message, Object context = null)
        {
            if (!_debug) return;
            Debug.Log($"<color=green> FROM: {this} | DONE: {message} </color>", context);
        }
        
        protected void Warning(string message, Object context = null)
        {
            if (!_warning) return;
            Debug.LogWarning($"<color=yellow> FROM: {this} | WARNING: {message} </color>", context);
        }

        protected void Error(string message, Object context = null)
        {
            if (!_error) return;
            Debug.LogError($"<color=red> FROM: {this} | ERROR: {message} </color>", context);
        }
        
        #endregion
        

        #region Private & Protected Fields

        // [SerializeField] private TMP_Text m_label;
        // [SerializeField] private TMP_Text m_description;
        
        [SerializeField, HideInInspector]
        protected bool _debug;
        [SerializeField, HideInInspector]
        protected bool _warning;
        [SerializeField, HideInInspector]
        protected bool _error;

        #endregion
    }
}

using UnityEngine;
using UnityEngine.Serialization;

namespace TheFundation.Runtime
{
    [CreateAssetMenu(
        fileName = "SettingDefinition",
        menuName = "TheFundation/Settings/Setting Definition")]
    public class SettingDefinition : ScriptableObject
    {
        [Header("Identification")]
        public string m_key; // ex: "settings_masterVolume"
        public SettingsCategory m_category;

        [Header("UI Display (fallback, no localization)")]
        public string m_label;  
        public string m_description;
        
        [Header("Localization Keys")]
        public string m_labelKey;
        public string m_descriptionKey;

        [Header("Type")]
        public SettingType m_type;

        [Header("Float Settings")]
        public float m_defaultFloat = 1f;
        public float m_minFloat = 0f;
        public float m_maxFloat = 1f;

        [Header("Bool Settings")]
        public bool m_defaultBool = true;

        [Header("Dropdown")]
        public string[] m_options;
        
        public bool m_showOnDesktop = true;
        public bool m_showOnMobile = true;
        public bool m_showOnConsole = true;
        
    }

    public enum SettingType
    {
        Float,
        Bool,
        Dropdown
    }
}
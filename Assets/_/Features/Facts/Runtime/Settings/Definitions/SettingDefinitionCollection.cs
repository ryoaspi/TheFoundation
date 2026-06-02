using UnityEngine;

namespace TheFundation.Runtime
{
    [CreateAssetMenu(
        fileName = "SettingsDefinitionCollection",
        menuName = "TheFundation/Settings/Settings Collection")]
    public class SettingsDefinitionCollection : ScriptableObject
    {
        public SettingDefinition[] m_Settings;

        public SettingDefinition Get(string key)
        {
            foreach (var s in m_Settings)
                if (s.m_key == key) return s;
            return null;
        }
    }
}
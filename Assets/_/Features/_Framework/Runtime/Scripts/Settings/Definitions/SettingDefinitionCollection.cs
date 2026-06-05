using UnityEngine;

namespace TheFoundation.Runtime
{
    [CreateAssetMenu(
        fileName = "SettingsDefinitionCollection",
        menuName = "TheFoundation/Settings/Settings Collection")]
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
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheFoundation.Runtime
{
    /// <summary>
    /// Binder logique pour les dropdowns.
    /// Appelé automatiquement par FBehaviour.
    /// </summary>
    public static class DropdownBinderLogic
    {
        #region Publics

        public static void Bind(Dropdown dropdown, SettingDefinition def)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(def.m_options));

            dropdown.value = SettingsService.GetDropdownIndex(def.m_key);

            dropdown.onValueChanged.AddListener(v =>
            {
                SettingsService.SetDropdownIndex(def.m_key, v);
            });
        }

        #endregion
    }
}
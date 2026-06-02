using UnityEngine.UI;

namespace TheFundation.Runtime
{
    /// <summary>
    /// Binder logique pour les toggles.
    /// Appelé automatiquement par FBehaviour.
    /// </summary>
    public static class ToggleBinderLogic
    {
        #region Publics

        public static void Bind(Toggle toggle, SettingDefinition def)
        {
            toggle.isOn = SettingsService.GetBool(def.m_key);

            toggle.onValueChanged.AddListener(v =>
            {
                SettingsService.SetBool(def.m_key, v);
            });
        }

        #endregion
    }
}
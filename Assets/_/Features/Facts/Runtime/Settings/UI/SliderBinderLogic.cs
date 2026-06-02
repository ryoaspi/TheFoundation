using UnityEngine.UI;

namespace TheFundation.Runtime
{
    /// <summary>
    /// Binder logique pour les sliders de paramètres.
    /// Non-MonoBehaviour. Appelé automatiquement par FBehaviour.
    /// </summary>
    public static class SliderBinderLogic
    {
        #region Publics

        public static void Bind(Slider slider, SettingDefinition def)
        {
            slider.minValue = def.m_minFloat;
            slider.maxValue = def.m_maxFloat;
            slider.value = SettingsService.GetFloat(def.m_key);

            slider.onValueChanged.AddListener(v =>
            {
                SettingsService.SetFloat(def.m_key, v);
            });
        }

        #endregion
    }
}
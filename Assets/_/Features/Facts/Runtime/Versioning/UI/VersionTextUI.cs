using UnityEngine;
using TMPro;

namespace TheFundation.Runtime
{
    /// <summary>
    /// VersionTextUI
    /// --------------
    /// Affiche la version du jeu dans un TMP_Text.
    /// - Peut afficher la version du build Unity
    /// - Ou la version interne du framework (VersionDefinition)
    /// - Ou les deux
    /// - Supporte la localisation via LocalizationManager
    /// </summary>
    public class VersionTextUI : FBehaviour
    {
        public enum VersionType
        {
            BuildVersion,      // Application.version
            FrameworkVersion,  // VersionService.FrameworkVersion
            Both               // Build + Framework
        }

        [Header("UI")]
        public TMP_Text m_text;

        [Header("Localisation")]
        [Tooltip("Clé de localisation (ex : ui.version). Utiliser {0} pour insérer la version.")]
        public string m_localizationKey = "ui.version";

        [Header("Version à afficher")]
        public VersionType m_versionType = VersionType.BuildVersion;


        #region Unity

        protected override void Start()
        {
            base.Start();

            if (m_text == null)
            {
                Debug.LogError("[VersionTextUI] Aucun TMP_Text assigné.", this);
                enabled = false;
                return;
            }

            UpdateText();
            LocalizationManager.OnLanguageChanged += UpdateText;
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= UpdateText;
        }

        #endregion


        #region Logic

        private void UpdateText()
        {
            // 1) Choisir la version à afficher
            string versionString;

            if (m_versionType == VersionType.BuildVersion)
            {
                versionString = VersionService.BuildVersion;
            }
            else if (m_versionType == VersionType.FrameworkVersion)
            {
                versionString = VersionService.FrameworkVersion;
            }
            else // Both
            {
                versionString = "Build " + VersionService.BuildVersion +
                                " | FW " + VersionService.FrameworkVersion;
            }

            // 2) Localisation (si la clé existe)
            if (!string.IsNullOrEmpty(m_localizationKey))
            {
                string localized = LocalizationManager.GetText(m_localizationKey);

                // Si la clé n'existe pas, LocalizationManager renvoie "[clé]"
                if (localized != "[" + m_localizationKey + "]")
                {
                    m_text.text = localized.Replace("{0}", versionString);
                    return;
                }
            }

            // 3) Fallback si pas de localisation valable
            m_text.text = "Version " + versionString;
        }

        #endregion
    }
}

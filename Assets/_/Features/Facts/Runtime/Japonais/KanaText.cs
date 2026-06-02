using TMPro;
using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// KanaText — affichage lecture seule de texte romaji converti en kana
    /// ---------------------------------------------------------------------
    /// Contrairement à KanaInputField (saisie interactive), KanaText
    /// est un composant d'affichage passif : il prend un texte romaji
    /// et l'affiche converti en kana dans un TMP_Text.
    ///
    /// Cas d'usage :
    ///   - Afficher le nom d'un personnage en kana
    ///   - Afficher une réponse joueur en lecture seule
    ///   - Afficher des mots de vocabulaire dans un mini-jeu
    ///   - Utiliser avec LocalizationManager (clé de loc → romaji → kana)
    ///
    /// Setup :
    ///   Ajouter sur un GameObject avec TMP_Text.
    ///   Renseigner m_romaji dans l'Inspector, ou appeler SetRomaji() par code.
    ///
    /// Compatibilité LocalizationManager :
    ///   Si m_useLocalizationKey = true, le composant lit sa valeur depuis
    ///   LocalizationManager.GetText(m_localizationKey) et la convertit.
    ///   Se met à jour automatiquement quand la langue change.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class KanaText : FBehaviour
    {
        #region Inspector

        [Header("Source du texte")]
        [Tooltip("Texte romaji à convertir (si pas de clé de localisation).")]
        [SerializeField] private string m_romaji;

        [Header("Localisation (optionnel)")]
        [Tooltip("Si true, lit le texte depuis LocalizationManager au lieu de m_romaji.")]
        [SerializeField] private bool m_useLocalizationKey = false;

        [Tooltip("Clé de localisation (ex: 'npc.name'). La valeur doit être en romaji.")]
        [SerializeField] private string m_localizationKey;

        [Header("Mode kana")]
        [SerializeField] private KanaConverter.KanaMode m_mode = KanaConverter.KanaMode.Hiragana;

        [Tooltip("Si true, affiche le romaji original en dessous du kana (furigana simplifié).")]
        [SerializeField] private bool m_showRomajiBelow = false;

        [SerializeField] private TMP_Text m_romajiLabel; // label secondaire optionnel

        #endregion

        #region Publics

        /// <summary>Change le texte romaji source et rafraîchit l'affichage.</summary>
        public void SetRomaji(string romaji)
        {
            m_useLocalizationKey = false;
            m_romaji = romaji;
            Refresh();
        }

        /// <summary>Change le mode kana et rafraîchit l'affichage.</summary>
        public void SetMode(KanaConverter.KanaMode mode)
        {
            m_mode = mode;
            Refresh();
        }

        /// <summary>Toggle hiragana ↔ katakana.</summary>
        public void ToggleMode()
        {
            SetMode(m_mode == KanaConverter.KanaMode.Hiragana
                ? KanaConverter.KanaMode.Katakana
                : KanaConverter.KanaMode.Hiragana);
        }

        #endregion

        #region Unity lifecycle

        private TMP_Text _text;

        protected override void Start()
        {
            base.Start();
            _text = GetComponent<TMP_Text>();
            Refresh();
        }

        /// <summary>
        /// Hook FBehaviour : rafraîchit si la langue change
        /// (utile quand m_useLocalizationKey = true).
        /// </summary>
        protected override void OnLocalizationChanged()
        {
            if (m_useLocalizationKey)
                Refresh();
        }

        #endregion

        #region Logique

        private void Refresh()
        {
            if (!_text) return;

            string source = ResolveSource();

            if (string.IsNullOrEmpty(source))
            {
                _text.text = string.Empty;
                if (m_romajiLabel) m_romajiLabel.text = string.Empty;
                return;
            }

            string kana = KanaConverter.Convert(source, m_mode);
            _text.text = kana;

            // Label romaji secondaire (furigana simplifié)
            if (m_showRomajiBelow && m_romajiLabel)
                m_romajiLabel.text = source;
            else if (m_romajiLabel)
                m_romajiLabel.text = string.Empty;
        }

        private string ResolveSource()
        {
            if (m_useLocalizationKey && !string.IsNullOrEmpty(m_localizationKey))
                return LocalizationManager.GetText(m_localizationKey);

            return m_romaji;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Prévisualisation en Editor sans lancer le jeu
            var txt = GetComponent<TMP_Text>();
            if (txt && !string.IsNullOrEmpty(m_romaji))
                txt.text = KanaConverter.Convert(m_romaji, m_mode);
        }
#endif
    }
}
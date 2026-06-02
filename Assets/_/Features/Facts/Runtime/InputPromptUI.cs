using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheFundation.Runtime
{
    /// <summary>
    /// InputPromptUI
    /// --------------
    /// Affiche automatiquement la bonne icône / texte selon :
    /// - le joueur (playerIndex)
    /// - l’action logique (actionName)
    /// - le control scheme actif (Keyboard, Xbox, PS, Switch, Touch…)
    /// 
    /// S’appuie sur :
    /// - PlayerInputSchemeBridge (event OnSchemeChanged)
    /// - InputIconDatabase (ScriptableObject)
    /// - TMP SpriteAssets (pour les balises <sprite>)
    /// 
    /// Usage :
    /// - Mettre ce script sur un GameObject UI
    /// - Assigner l’image, le TMP_Text, la database et les SpriteAssets
    /// - Entrer l’action (ex : "Jump", "Interact", "Attack")
    /// </summary>
    public class InputPromptUI : MonoBehaviour
    {
        #region Publics

        [Header("Joueur Cible (0 = P1, 1 = P2, etc.)")]
        public int m_PlayerIndex = 0;

        [Header("Nom de l'action (clé dans InputIconDatabase)")]
        public string m_ActionName;

        [Header("Cibles UI (optionnelles)")]
        public Image m_IconTarget;
        public TMP_Text m_LabelTarget;

        [Header("Bases d'icônes selon le scheme")]
        public TMP_SpriteAsset m_KeyboardSprites;
        public TMP_SpriteAsset m_XboxSprites;
        public TMP_SpriteAsset m_PsSprites;
        public TMP_SpriteAsset m_SwitchSprites;
        public TMP_SpriteAsset m_GenericSprites;
        public TMP_SpriteAsset m_TouchSprites;

        [Header("Base de données InputIconDatabase")]
        public InputIconDatabase m_IconDatabase;

        #endregion


        #region Unity API

        private void OnEnable()
        {
            PlayerInputSchemeBridge.OnSchemeChanged += HandleSchemeChanged;
            Refresh();
        }

        private void OnDisable()
        {
            PlayerInputSchemeBridge.OnSchemeChanged -= HandleSchemeChanged;
        }

        private void Start()
        {
            Refresh();
        }

        #endregion


        #region Utils

        private void HandleSchemeChanged(int playerIndex, string scheme, string brand)
        {
            if (playerIndex != m_PlayerIndex)
                return;

            Refresh(scheme);
        }

        #endregion


        #region Main Methods

        /// <summary>
        /// Rafraîchit l'affichage selon le scheme fourni ou celui stocké dans les Facts.
        /// </summary>
        private void Refresh(string forcedScheme = null)
        {
            if (m_IconDatabase == null)
                return;

            // 1) Récupération du scheme
            string scheme = forcedScheme;

            if (string.IsNullOrEmpty(scheme))
                GameManager.Facts.TryGetFact($"p{m_PlayerIndex}_inputScheme", out scheme);

            if (string.IsNullOrEmpty(scheme))
                scheme = "KeyboardMouse";

            // 2) Essayer de récupérer le set du scheme
            if (!m_IconDatabase.TryGet(scheme, out var iconSet))
                return;

            // 3) Sprite/Icon
            if (m_IconTarget)
            {
                var icon = iconSet.icons.Find(i => i.action == m_ActionName)?.sprite;
                m_IconTarget.enabled = icon != null;
                m_IconTarget.sprite  = icon;
            }

            // 4) Texte + sprite embed TMP
            if (m_LabelTarget)
            {
                var label  = iconSet.labels.Find(l => l.action == m_ActionName)?.label ?? m_ActionName;
                var spName = iconSet.sprites.Find(sp => sp.action == m_ActionName)?.spriteName;

                var spriteAsset = SelectSpriteAssetForScheme(scheme);

                if (spriteAsset && !string.IsNullOrEmpty(spName))
                {
                    m_LabelTarget.spriteAsset = spriteAsset;
                    m_LabelTarget.text = $"{label} <sprite name=\"{spName}\">";
                }
                else
                {
                    m_LabelTarget.text = label;
                }
            }
        }


        /// <summary>
        /// Choisit le bon TMP SpriteAsset selon le control scheme utilisé.
        /// </summary>
        private TMP_SpriteAsset SelectSpriteAssetForScheme(string scheme)
        {
            return scheme switch
            {
                "KeyboardMouse"         => m_KeyboardSprites,
                "XboxController"        => m_XboxSprites,
                "PlayStationController" => m_PsSprites,
                "SwitchController"      => m_SwitchSprites,
                "Touch"                 => m_TouchSprites,
                _                       => m_GenericSprites,
            };
        }

        #endregion


        #region Private

        // Rien ici pour l’instant.

        #endregion
    }
}

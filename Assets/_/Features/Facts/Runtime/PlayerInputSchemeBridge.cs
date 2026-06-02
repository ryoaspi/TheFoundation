using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFundation.Runtime
{
    /// <summary>
    /// Module d’identification du device utilisé par chaque joueur, basé sur PlayerInput.
    /// - Support multi-joueur (playerIndex)
    /// - Détection du Control Scheme actif
    /// - Détection marque manette (Xbox, PlayStation, Switch)
    /// - Met à jour les Facts :
    ///     p{playerIndex}_inputScheme
    ///     p{playerIndex}_deviceType
    ///     p{playerIndex}_brand
    /// - Envoie un event global OnSchemeChanged
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    [DefaultExecutionOrder(-80)]
    public class PlayerInputSchemeBridge : MonoBehaviour
    {
        #region Publics

        /// <summary>
        /// Event global appelé quand le device d’un joueur change.
        /// Params : (playerIndex, scheme, brand)
        /// </summary>
        public static event Action<int, string, string> OnSchemeChanged;

        #endregion

        #region API Unity

        private PlayerInput _playerInput;
        private string _currentScheme = "";
        private string _currentBrand  = "";

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();

            // Seed initial : récupérer le scheme actuel au démarrage.
            var mapped = MapControlScheme(_playerInput.currentControlScheme);
            ApplyMappedScheme(_playerInput.playerIndex, mapped.scheme, mapped.brand);
        }

        private void OnEnable()
        {
            _playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDisable()
        {
            _playerInput.onControlsChanged -= OnControlsChanged;
        }

        #endregion

        #region Utils

        /// <summary>
        /// Callback quand Unity change automatiquement le Control Scheme.
        /// </summary>
        private void OnControlsChanged(PlayerInput playerInput)
        {
            var mapped = MapControlScheme(playerInput.currentControlScheme);
            ApplyMappedScheme(playerInput.playerIndex, mapped.scheme, mapped.brand);
        }

        /// <summary>
        /// Associe un "control scheme" Unity à un scheme interne + marque manette.
        /// </summary>
        private (string scheme, string brand) MapControlScheme(string piScheme)
        {
            if (string.IsNullOrEmpty(piScheme))
                piScheme = "Keyboard&Mouse";

            string normalized = piScheme.Replace(" ", "").Replace("&", "").ToLower();

            // 1) Clavier + souris
            if (normalized.Contains("keyboard") || normalized.Contains("mouse"))
                return ("KeyboardMouse", "");

            // 2) Touchscreen
            if (normalized.Contains("touch"))
                return ("Touch", "");

            // 3) VR / XR
            if (normalized.Contains("xr") || normalized.Contains("vr"))
                return ("XRController", "");

            // 4) Gamepad → on devine la marque
            if (normalized.Contains("gamepad") || normalized.Contains("controller"))
                return DetectGamepadBrand();

            // Fallback
            return ("GenericController", "");
        }

        /// <summary>
        /// Détecte la marque de la manette selon le nom du device.
        /// </summary>
        private (string scheme, string brand) DetectGamepadBrand()
        {
            foreach (var gp in Gamepad.all)
            {
                string n = (gp.name ?? "").ToLower();
                string d = (gp.displayName ?? "").ToLower();

                if (n.Contains("xbox") || d.Contains("xbox") || n.Contains("xinput"))
                    return ("XboxController", "Xbox");

                if (n.Contains("dual") || d.Contains("playstation") || n.Contains("ps4") || n.Contains("ps5"))
                    return ("PlayStationController", "PlayStation");

                if (n.Contains("switch"))
                    return ("SwitchController", "Switch");
            }

            return ("GenericController", "Generic");
        }

        #endregion

        #region Main Methods

        /// <summary>
        /// Attribue le scheme et met à jour Facts + Event global.
        /// </summary>
        private void ApplyMappedScheme(int playerIndex, string scheme, string brand)
        {
            // Si rien n’a changé → on évite de spammer
            if (_currentScheme == scheme && _currentBrand == brand)
                return;

            _currentScheme = scheme;
            _currentBrand  = brand;

            // 1) Update Facts
            GameManager.Facts.SetFact($"p{playerIndex}_inputScheme", scheme, FactDictionary.FactPersistence.Normal);
            GameManager.Facts.SetFact($"p{playerIndex}_brand",       brand,  FactDictionary.FactPersistence.Normal);
            GameManager.Facts.SetFact($"p{playerIndex}_deviceType",  MapToDeviceType(scheme), FactDictionary.FactPersistence.Normal);

            // 2) Event global (pour UI prompts, settings, etc.)
            OnSchemeChanged?.Invoke(playerIndex, scheme, brand);

            // 3) Debug (désactivable)
            Debug.Log($"[InputBridge] P{playerIndex} → Scheme={scheme}, Brand={brand}");
        }

        /// <summary>
        /// Map scheme → device type global pour UI et plateforme.
        /// </summary>
        private string MapToDeviceType(string scheme)
        {
            return scheme switch
            {
                "KeyboardMouse"         => "PC",
                "Touch"                 => "Mobile",
                "XboxController"        => "Xbox",
                "PlayStationController" => "PlayStation",
                "SwitchController"      => "Switch",
                _                       => "PC",
            };
        }

        #endregion

        #region Private

        // Rien d’autre nécessaire ici pour le moment.

        #endregion
    }
}

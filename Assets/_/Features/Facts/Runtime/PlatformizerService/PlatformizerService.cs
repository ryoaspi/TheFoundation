using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// PlatformizerService
    /// -------------------
    /// Service non-MonoBehaviour détectant :
    /// - Système d’exploitation (Windows, macOS, Linux, Android, iOS…)
    /// - Famille de plateforme (Desktop, Mobile, Console)
    /// - Capacités d’entrée (souris, clavier, gamepad, tactile)
    ///
    /// Toutes les informations sont stockées dans FactDictionary (V1).
    /// </summary>
    public static class PlatformizerService
    {
        #region Publics

        public static bool IsInitialized { get; private set; }

        #endregion


        #region Initialization

        public static void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;

            DetectOperatingSystem();
            DetectPlatformFamily();
            DetectInputCapabilities();
        }

        #endregion


        #region Main Methods

        private static void DetectOperatingSystem()
        {
            string os = SystemInfo.operatingSystem.ToLower();

            string cleanOS =
                os.Contains("windows") ? "Windows" :
                (os.Contains("mac") || os.Contains("osx")) ? "macOS" :
                os.Contains("linux") ? "Linux" :
                os.Contains("android") ? "Android" :
                os.Contains("ios") ? "iOS" :
                "Unknown";

            GameManager.Facts.SetFact("platform_os", cleanOS, FactDictionary.FactPersistence.Persistent);
        }


        private static void DetectPlatformFamily()
        {
            string family =
                Application.platform == RuntimePlatform.WebGLPlayer ? "Web" :
                SystemInfo.deviceType == DeviceType.Handheld ? "Mobile" :
                "Desktop";

            GameManager.Facts.SetFact("platform_family", family, FactDictionary.FactPersistence.Persistent);
        }


        private static void DetectInputCapabilities()
        {
            bool hasKeyboard = SystemInfo.deviceType == DeviceType.Desktop;
            bool hasMouse    = hasKeyboard;
            bool hasTouch    = Input.touchSupported;
            bool hasGyro     = SystemInfo.supportsGyroscope;

            // Stockage dans les Facts
            GameManager.Facts.SetFact("input_hasKeyboard", hasKeyboard, FactDictionary.FactPersistence.Normal);
            GameManager.Facts.SetFact("input_hasMouse", hasMouse, FactDictionary.FactPersistence.Normal);
            GameManager.Facts.SetFact("input_hasTouch", hasTouch, FactDictionary.FactPersistence.Normal);
            GameManager.Facts.SetFact("input_hasGyro", hasGyro, FactDictionary.FactPersistence.Normal);
        }

        #endregion
    }
}

using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// VersionService
    /// ---------------
    /// - Fournit la version interne du framework (VersionDefinition)
    /// - Fournit la version du build (Application.version)
    /// - Stocke la version interne dans les Facts pour détecter les changements
    /// </summary>
    public static class VersionService
    {
        private const string VERSION_FACT_KEY = "game_version";

        /// <summary>
        /// ScriptableObject qui contient la version du framework / projet.
        /// À assigner depuis un asset (ex: VersionDefinition.asset).
        /// </summary>
        public static VersionDefinition m_definition;

        /// <summary>
        /// Version interne du framework / projet.
        /// </summary>
        public static string FrameworkVersion
        {
            get
            {
                if (m_definition != null)
                    return m_definition.m_versionString;
                return "0.0.0";
            }
        }

        /// <summary>
        /// Version du build Unity (Player Settings → Version).
        /// </summary>
        public static string BuildVersion
        {
            get { return Application.version; }
        }

        /// <summary>
        /// À appeler au démarrage (ex: depuis GameManager.Awake()).
        /// </summary>
        public static void Initialize()
        {
            if (m_definition == null)
            {
                Debug.LogWarning("[VersionService] Aucun VersionDefinition assigné.");
                return;
            }

            // Lire l'ancienne version stockée dans les Facts
            string previousVersion = "none";
            GameManager.Facts.TryGetFact(VERSION_FACT_KEY, out previousVersion);

            // Si différente → événement de changement
            if (previousVersion != FrameworkVersion)
            {
                OnVersionChanged(previousVersion, FrameworkVersion);
            }

            // Stocker la version actuelle dans les Facts (persistant)
            GameManager.Facts.SetFact(
                VERSION_FACT_KEY,
                FrameworkVersion,
                FactDictionary.FactPersistence.Persistent);
        }

        /// <summary>
        /// Appelé automatiquement si la version interne change.
        /// </summary>
        private static void OnVersionChanged(string oldVersion, string newVersion)
        {
            Debug.Log($"[VersionService] Version changée : {oldVersion} -> {newVersion}");

            // Ici, plus tard, tu pourras :
            // - migrer certaines données de sauvegarde
            // - reset des Facts obsolètes
            // - offrir un bonus de mise à jour, etc.
        }
    }
}

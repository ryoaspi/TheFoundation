using TheFundation.Runtime.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheFundation.Runtime
{
    /// <summary>
    /// SceneService — gestion des transitions de scènes
    /// --------------------------------------------------
    /// Centralise tous les LoadScene du projet.
    /// Remplace les appels directs à SceneManager.LoadScene() éparpillés
    /// (TitleAction, TitleRouter, etc.)
    ///
    /// Philosophie :
    ///   - Zéro coroutine, zéro Task — tout est synchrone ou via callbacks Unity
    ///   - Sauvegarde automatique avant chaque transition
    ///   - Events EventBus (OnSceneLoading / OnSceneLoaded) pour l'UI et l'audio
    ///   - Support du chargement additif (overlays, HUD persistant)
    ///
    /// Setup :
    ///   Aucun GameObject requis — service statique pur.
    ///   Appeler SceneService.Initialize() dans GameManager.Awake()
    ///   après les autres services.
    ///
    /// Usage :
    ///   SceneService.Load("GameScene");
    ///   SceneService.Load("GameScene", saveBeforeLoad: false);
    ///   SceneService.Reload();
    ///   SceneService.LoadAdditive("HUDScene");
    ///   SceneService.Unload("HUDScene");
    /// </summary>
    public static class SceneService
    {
        #region Publics

        public static bool IsInitialized { get; private set; }

        /// <summary>Nom de la scène actuellement active (scène principale).</summary>
        public static string CurrentScene => SceneManager.GetActiveScene().name;

        #endregion

        #region Initialisation

        /// <summary>
        /// À appeler dans GameManager.Awake() après les autres services.
        /// </summary>
        public static void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;

            // S'abonner aux callbacks Unity natifs
            SceneManager.sceneLoaded   += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

#if UNITY_EDITOR
            Debug.Log("[SceneService] Initialisé.");
#endif
        }

        #endregion

        #region API publique — chargement principal

        /// <summary>
        /// Charge une scène en mode Single (remplace la scène courante).
        /// Sauvegarde automatiquement les Facts avant le chargement si demandé.
        /// </summary>
        /// <param name="sceneName">Nom exact de la scène (Build Settings).</param>
        /// <param name="saveBeforeLoad">Si true, sauvegarde les Facts avant de partir.</param>
        public static void Load(string sceneName, bool saveBeforeLoad = true)
        {
            if (!Validate(sceneName)) return;

            if (saveBeforeLoad)
                FactSaveSystem.SaveToFile(GameManager.Facts);

            EventBus.Raise(new OnSceneLoading { sceneName = sceneName });

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        /// <summary>
        /// Recharge la scène courante.
        /// </summary>
        public static void Reload(bool saveBeforeLoad = false)
        {
            Load(CurrentScene, saveBeforeLoad);
        }

        /// <summary>
        /// Charge depuis un index Build Settings.
        /// </summary>
        public static void LoadByIndex(int buildIndex, bool saveBeforeLoad = true)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[SceneService] Index {buildIndex} hors des Build Settings.");
                return;
            }

            if (saveBeforeLoad)
                FactSaveSystem.SaveToFile(GameManager.Facts);

            string sceneName = System.IO.Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(buildIndex));

            EventBus.Raise(new OnSceneLoading { sceneName = sceneName });

            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
        }

        #endregion

        #region API publique — additif (overlay, HUD)

        /// <summary>
        /// Charge une scène en mode additif (par-dessus la scène courante).
        /// Utile pour les overlays, écrans de pause, HUD séparé.
        /// </summary>
        public static void LoadAdditive(string sceneName)
        {
            if (!Validate(sceneName)) return;

            EventBus.Raise(new OnSceneLoading { sceneName = sceneName });

            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Décharge une scène additive.
        /// </summary>
        public static void Unload(string sceneName)
        {
            if (!IsSceneLoaded(sceneName))
            {
                Debug.LogWarning($"[SceneService] Scène '{sceneName}' non chargée, impossible de décharger.");
                return;
            }

            SceneManager.UnloadSceneAsync(sceneName);
        }

        /// <summary>
        /// Retourne true si une scène additive est actuellement chargée.
        /// </summary>
        public static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }
            return false;
        }

        #endregion

        #region API publique — slots de sauvegarde + scène

        /// <summary>
        /// Charge depuis un slot de sauvegarde puis change de scène.
        /// Combinaison fréquente depuis TitleAction.ContinueGame().
        /// </summary>
        public static void LoadFromSlotAndScene(int slot, string sceneName)
        {
            if (!Validate(sceneName)) return;

            if (!FactSaveSystem.SlotExist(slot))
            {
                Debug.LogWarning($"[SceneService] Slot {slot} inexistant.");
                return;
            }

            FactSaveSystem.LoadFromSlot(GameManager.Facts, slot);
            EventBus.Raise(new OnGameLoaded { slot = slot });

            Load(sceneName, saveBeforeLoad: false);
        }

        #endregion

        #region Callbacks Unity natifs

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EventBus.Raise(new OnSceneLoaded { sceneName = scene.name });

#if UNITY_EDITOR
            Debug.Log($"[SceneService] Scène chargée : {scene.name} ({mode})");
#endif
        }

        private static void OnSceneUnloaded(Scene scene)
        {
#if UNITY_EDITOR
            Debug.Log($"[SceneService] Scène déchargée : {scene.name}");
#endif
        }

        #endregion

        #region Privés

        private static bool Validate(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneService] Nom de scène vide.");
                return false;
            }
            return true;
        }

        #endregion
    }
}
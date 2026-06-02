using System.Collections.Generic;
using TheFundation.Runtime.Events;
using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// AudioService — banque de sons centralisée
    /// ------------------------------------------
    /// Remplace les AudioSource éparpillés sur chaque GameObject.
    /// Un seul point d'entrée pour tous les sons du jeu.
    ///
    /// Philosophie :
    ///   - Zéro coroutine, zéro Task
    ///   - Pool d'AudioSources géré en interne (taille configurable)
    ///   - Sons définis dans des AudioDefinition (ScriptableObjects)
    ///   - Intégration SettingsService (volume master, sfx, music)
    ///   - Musique : une seule source dédiée (crossfade sans coroutine via Update)
    ///
    /// Setup :
    ///   1. Créer des AudioDefinition (clic droit → TheFundation/Audio/...)
    ///   2. Créer un AudioCollection et y glisser les définitions
    ///   3. Assigner l'AudioCollection sur le GameManager
    ///   4. Appeler AudioService.Initialize() dans GameManager.Awake()
    ///
    /// Usage :
    ///   AudioService.PlaySfx("sfx_jump");
    ///   AudioService.PlayMusic("music_menu");
    ///   AudioService.StopMusic();
    ///   AudioService.SetSfxVolume(0.8f);
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class AudioService : MonoBehaviour
    {
        #region Singleton interne

        public static AudioService Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

        #endregion

        #region Inspector

        [Header("Pool de sources SFX")]
        [Tooltip("Nombre de sons pouvant jouer simultanément.")]
        [SerializeField] private int _poolSize = 12;

        [Header("Source musique")]
        [SerializeField] private AudioSource _musicSource;

        [Header("Crossfade musique (sans coroutine)")]
        [Tooltip("Vitesse du fondu enchaîné entre deux musiques (unités/seconde).")]
        [SerializeField] private float _crossfadeSpeed = 1.5f;

        #endregion

        #region Données internes

        private static AudioCollection _collection;

        // Pool de sources SFX
        private AudioSource[] _pool;
        private int _poolIndex;

        // Crossfade sans coroutine
        private AudioSource _musicSourceB;   // source secondaire pour le fondu
        private bool _isCrossfading;
        private float _crossfadeTarget;      // volume cible de _musicSource
        private float _crossfadeTargetB;     // volume cible de _musicSourceB

        // Volumes courants
        private float _masterVolume = 1f;
        private float _sfxVolume    = 1f;
        private float _musicVolume  = 1f;

        #endregion

        #region Initialisation

        /// <summary>
        /// Appelé par GameManager.Awake() après SettingsService.
        /// </summary>
        public static void Initialize(AudioCollection collection)
        {
            if (IsInitialized) return;
            if (collection == null)
            {
                Debug.LogError("[AudioService] AudioCollection nulle.");
                return;
            }

            _collection = collection;

            // Créer le GameObject portant le service
            var go = new GameObject("[AudioService]");
            DontDestroyOnLoad(go);
            var svc = go.AddComponent<AudioService>();
            Instance = svc;
            svc.Setup();

            IsInitialized = true;

#if UNITY_EDITOR
            Debug.Log("[AudioService] Initialisé.");
#endif
        }

        private void Setup()
        {
            // Pool SFX
            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool[i] = src;
            }

            // Source musique principale
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
            }

            // Source musique B (crossfade)
            _musicSourceB = gameObject.AddComponent<AudioSource>();
            _musicSourceB.playOnAwake = false;
            _musicSourceB.loop = true;
            _musicSourceB.volume = 0f;

            // Lire les volumes depuis les Settings
            RefreshVolumes();
        }

        #endregion

        #region Update — crossfade sans coroutine

        private void Update()
        {
            if (!_isCrossfading) return;

            float step = _crossfadeSpeed * Time.deltaTime;

            _musicSource.volume  = MoveToward(_musicSource.volume,  _crossfadeTarget,  step);
            _musicSourceB.volume = MoveToward(_musicSourceB.volume, _crossfadeTargetB, step);

            // Crossfade terminé ?
            if (Approx(_musicSource.volume, _crossfadeTarget) &&
                Approx(_musicSourceB.volume, _crossfadeTargetB))
            {
                _isCrossfading = false;

                // Si la source principale est maintenant à 0, on bascule
                if (_crossfadeTarget <= 0f)
                {
                    // Swap : B devient A
                    var clip = _musicSourceB.clip;
                    _musicSource.clip   = clip;
                    _musicSource.volume = _musicVolume * _masterVolume;
                    _musicSource.time   = _musicSourceB.time;
                    _musicSource.Play();

                    _musicSourceB.Stop();
                    _musicSourceB.clip   = null;
                    _musicSourceB.volume = 0f;
                }
                else if (_crossfadeTargetB <= 0f)
                {
                    _musicSourceB.Stop();
                    _musicSourceB.clip   = null;
                    _musicSourceB.volume = 0f;
                }
            }
        }

        #endregion

        #region API publique — SFX

        /// <summary>
        /// Joue un son one-shot depuis la banque.
        /// </summary>
        public static void PlaySfx(string key)
        {
            if (!Check()) return;
            var def = _collection.Get(key);
            if (def == null) { Warn(key); return; }

            var src = Instance.GetPooledSource();
            src.clip        = def.GetClip();
            src.volume      = def.volume * Instance._sfxVolume * Instance._masterVolume;
            src.pitch       = def.GetPitch();
            src.spatialBlend = 0f;
            src.Play();

            EventBus.Raise(new OnSoundPlayed { soundKey = key });
        }

        /// <summary>
        /// Joue un son one-shot positionné dans l'espace 3D.
        /// </summary>
        public static void PlaySfxAt(string key, Vector3 position)
        {
            if (!Check()) return;
            var def = _collection.Get(key);
            if (def == null) { Warn(key); return; }

            var src = Instance.GetPooledSource();
            src.clip         = def.GetClip();
            src.volume       = def.volume * Instance._sfxVolume * Instance._masterVolume;
            src.pitch        = def.GetPitch();
            src.spatialBlend = 1f;
            src.transform.position = position;
            src.Play();

            EventBus.Raise(new OnSoundPlayed { soundKey = key });
        }

        #endregion

        #region API publique — Musique

        /// <summary>
        /// Lance une musique avec crossfade.
        /// Si la même musique joue déjà, ne fait rien.
        /// </summary>
        public static void PlayMusic(string key)
        {
            if (!Check()) return;
            var def = _collection.Get(key);
            if (def == null) { Warn(key); return; }

            var clip = def.GetClip();

            // Déjà en train de jouer cette musique ?
            if (Instance._musicSource.clip == clip && Instance._musicSource.isPlaying)
                return;

            float targetVol = def.volume * Instance._musicVolume * Instance._masterVolume;

            if (!Instance._musicSource.isPlaying)
            {
                // Pas de musique en cours → démarrage direct
                Instance._musicSource.clip   = clip;
                Instance._musicSource.volume = targetVol;
                Instance._musicSource.Play();
            }
            else
            {
                // Crossfade : A → 0, B (nouvelle) → targetVol
                Instance._musicSourceB.clip   = clip;
                Instance._musicSourceB.volume = 0f;
                Instance._musicSourceB.Play();

                Instance._crossfadeTarget  = 0f;
                Instance._crossfadeTargetB = targetVol;
                Instance._isCrossfading    = true;
            }

            EventBus.Raise(new OnMusicChanged { trackKey = key });
        }

        /// <summary>
        /// Stoppe la musique avec fondu sortant.
        /// </summary>
        public static void StopMusic()
        {
            if (!Check()) return;
            if (!Instance._musicSource.isPlaying) return;

            Instance._crossfadeTarget  = 0f;
            Instance._crossfadeTargetB = 0f;
            Instance._isCrossfading    = true;
        }

        /// <summary>
        /// Pause / reprise de la musique.
        /// </summary>
        public static void PauseMusic()  { if (Check()) Instance._musicSource.Pause(); }
        public static void ResumeMusic() { if (Check()) Instance._musicSource.UnPause(); }

        #endregion

        #region API publique — Volumes

        public static void SetMasterVolume(float v) { if (Check()) { Instance._masterVolume = Mathf.Clamp01(v); Instance.ApplyVolumes(); } }
        public static void SetSfxVolume(float v)    { if (Check()) { Instance._sfxVolume    = Mathf.Clamp01(v); } }
        public static void SetMusicVolume(float v)  { if (Check()) { Instance._musicVolume  = Mathf.Clamp01(v); Instance.ApplyMusicVolume(); } }

        public static float GetMasterVolume() => Check() ? Instance._masterVolume : 1f;
        public static float GetSfxVolume()    => Check() ? Instance._sfxVolume    : 1f;
        public static float GetMusicVolume()  => Check() ? Instance._musicVolume  : 1f;

        #endregion

        #region Privés

        private AudioSource GetPooledSource()
        {
            // Cherche d'abord une source libre
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying)
                    return _pool[i];
            }
            // Toutes occupées → on réutilise la plus ancienne (round-robin)
            _poolIndex = (_poolIndex + 1) % _pool.Length;
            _pool[_poolIndex].Stop();
            return _pool[_poolIndex];
        }

        private void RefreshVolumes()
        {
            _masterVolume = SettingsService.GetFloat("settings_masterVolume");
            _sfxVolume    = SettingsService.GetFloat("settings_sfxVolume");
            _musicVolume  = SettingsService.GetFloat("settings_musicVolume");
            ApplyMusicVolume();
        }

        private void ApplyVolumes()
        {
            ApplyMusicVolume();
        }

        private void ApplyMusicVolume()
        {
            if (!_isCrossfading && _musicSource != null)
                _musicSource.volume = _musicVolume * _masterVolume;
        }

        private static bool Check()
        {
            if (IsInitialized) return true;
            Debug.LogWarning("[AudioService] Non initialisé. Appeler Initialize() depuis GameManager.");
            return false;
        }

        private static void Warn(string key)
            => Debug.LogWarning($"[AudioService] Son '{key}' introuvable dans la collection.");

        private static float MoveToward(float current, float target, float step)
        {
            if (current < target) return Mathf.Min(current + step, target);
            if (current > target) return Mathf.Max(current - step, target);
            return target;
        }

        private static bool Approx(float a, float b)
            => Mathf.Abs(a - b) < 0.001f;

        #endregion
    }

    // ═══════════════════════════════════════════════════════════
    //  ScriptableObjects associés
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Définit un son : clip(s), volume, pitch, variation aléatoire.
    /// Un seul asset par "type de son" (ex: sfx_jump, music_menu).
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioDefinition",
        menuName = "TheFundation/Audio/Audio Definition")]
    public class AudioDefinition : ScriptableObject
    {
        [Header("Identification")]
        public string m_key; // ex: "sfx_jump", "music_menu"

        [Header("Clips (si plusieurs → aléatoire à chaque appel)")]
        public AudioClip[] m_clips;

        [Header("Volume & Pitch")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitch = 1f;

        [Header("Variation aléatoire")]
        [Range(0f, 0.5f)] public float pitchVariation = 0f;
        [Range(0f, 0.3f)] public float volumeVariation = 0f;

        public AudioClip GetClip()
        {
            if (m_clips == null || m_clips.Length == 0) return null;
            return m_clips[Random.Range(0, m_clips.Length)];
        }

        public float GetPitch()
            => pitch + Random.Range(-pitchVariation, pitchVariation);

        public float GetVolume()
            => Mathf.Clamp01(volume + Random.Range(-volumeVariation, volumeVariation));
    }

    /// <summary>
    /// Regroupe toutes les AudioDefinition du projet.
    /// Un seul asset assigné sur le GameManager.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioCollection",
        menuName = "TheFundation/Audio/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        public AudioDefinition[] m_definitions;

        public AudioDefinition Get(string key)
        {
            if (m_definitions == null) return null;
            foreach (var d in m_definitions)
                if (d != null && d.m_key == key) return d;
            return null;
        }
    }
}
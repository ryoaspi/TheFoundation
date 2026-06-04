namespace TheFoundation.Runtime.Events
{
    // ═══════════════════════════════════════════════════════════
    //  GameEvents.cs — Catalogue des events du framework
    //  Un seul fichier pour tous les events "built-in".
    //  Le jeu peut en définir d'autres dans son propre namespace.
    // ═══════════════════════════════════════════════════════════

    // ─── Scènes ─────────────────────────────────────────────────

    /// <summary>Émis juste avant le chargement d'une scène.</summary>
    public struct OnSceneLoading : IGameEvent
    {
        public string sceneName;
    }

    /// <summary>Émis dès que la nouvelle scène est active.</summary>
    public struct OnSceneLoaded : IGameEvent
    {
        public string sceneName;
    }

    // ─── Langue ─────────────────────────────────────────────────

    /// <summary>Émis par LocalizationManager quand la langue change.</summary>
    public struct OnLanguageChanged : IGameEvent
    {
        public string previousLang;
        public string newLang;
    }

    // ─── Sauvegarde ─────────────────────────────────────────────

    /// <summary>Émis après une sauvegarde réussie (fichier principal ou slot).</summary>
    public struct OnGameSaved : IGameEvent
    {
        /// <summary>-1 = fichier principal, 0-9 = slot numéroté.</summary>
        public int slot;
    }

    /// <summary>Émis après un chargement réussi.</summary>
    public struct OnGameLoaded : IGameEvent
    {
        public int slot;
    }

    // ─── Audio ──────────────────────────────────────────────────

    /// <summary>Émis quand un son commence à jouer.</summary>
    public struct OnSoundPlayed : IGameEvent
    {
        public string soundKey;
    }

    /// <summary>Émis quand la musique change.</summary>
    public struct OnMusicChanged : IGameEvent
    {
        public string trackKey;
    }

    // ─── Goals ──────────────────────────────────────────────────

    // Note : GoalEvents existants peuvent migrer ici progressivement.
    // Pour l'instant on ajoute un bridge dans GoalEvents (voir commentaire).

    // ─── Settings ───────────────────────────────────────────────

    /// <summary>Émis quand un réglage change de valeur.</summary>
    public struct OnSettingChanged : IGameEvent
    {
        public string key;
    }

    // ─── Plateforme ─────────────────────────────────────────────

    /// <summary>Émis quand le device d'input d'un joueur change.</summary>
    public struct OnInputSchemeChanged : IGameEvent
    {
        public int playerIndex;
        public string scheme;
        public string brand;
    }
}
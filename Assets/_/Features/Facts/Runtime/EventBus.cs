using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheFoundation.Runtime
{
    /// <summary>
    /// EventBus — bus d'événements centralisé
    /// ----------------------------------------
    /// Remplace la prolifération de "static event Action" sur chaque classe.
    /// Toutes les communications inter-systèmes passent par ici.
    ///
    /// Philosophie :
    ///   - Zéro allocation sur le chemin chaud (raise)
    ///   - Zéro coroutine, zéro Task
    ///   - Découplage total : l'émetteur ne connaît pas les récepteurs
    ///   - Compatible avec le FactDictionary : les events peuvent transporter
    ///     des données sans créer de dépendance entre systèmes
    ///
    /// Usage émetteur :
    ///   EventBus.Raise(new OnEnemyKilled { enemyId = "goblin", reward = 50 });
    ///
    /// Usage récepteur (dans OnEnable / OnDisable) :
    ///   EventBus.Subscribe<OnEnemyKilled>(HandleEnemyKilled);
    ///   EventBus.Unsubscribe<OnEnemyKilled>(HandleEnemyKilled);
    ///
    /// Définir un event :
    ///   public struct OnEnemyKilled : IGameEvent
    ///   {
    ///       public string enemyId;
    ///       public int reward;
    ///   }
    ///
    /// Convention de nommage :
    ///   - Structs (valeur, zéro GC) préfixés "On" → OnEnemyKilled, OnSceneLoaded
    ///   - Placés dans le namespace TheFundation.Runtime.Events
    /// </summary>
    public static class EventBus
    {
        #region Core

        // Dictionnaire typé : Type → liste de handlers
        // On stocke des Action<object> pour éviter la réflexion au Raise
        private static readonly Dictionary<Type, List<Action<object>>> _handlers = new();

        // Buffer de suppression différée (évite de modifier la liste pendant l'itération)
        private static readonly List<(Type type, Action<object> handler)> _pendingRemove = new();
        private static bool _isRaising = false;

        #endregion

        #region API publique

        /// <summary>
        /// S'abonner à un type d'event.
        /// Appeler dans OnEnable, jamais dans Awake (ordre d'initialisation).
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Action<object>>();
                _handlers[type] = list;
            }

            // Wrapper qui caste l'objet vers T — alloué une seule fois à l'abonnement
            list.Add(WrapHandler(handler));
        }

        /// <summary>
        /// Se désabonner. Appeler dans OnDisable / OnDestroy.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
                return;

            // Trouver le wrapper correspondant au handler original
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Target is HandlerWrapper<T> wrapper && wrapper.Original == handler)
                {
                    if (_isRaising)
                        _pendingRemove.Add((type, list[i]));
                    else
                        list.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Émet un event vers tous les abonnés.
        /// Appel synchrone direct — aucune allocation sur le chemin chaud.
        /// </summary>
        public static void Raise<T>(T gameEvent) where T : struct, IGameEvent
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list) || list.Count == 0)
                return;

            _isRaising = true;

            // Itération sur une copie de la liste pour éviter les bugs si un handler
            // modifie les abonnements pendant le Raise
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    list[i](gameEvent);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Exception dans le handler de {type.Name} : {e}");
                }
            }

            _isRaising = false;

            // Appliquer les suppressions différées
            if (_pendingRemove.Count > 0)
            {
                foreach (var (t, h) in _pendingRemove)
                {
                    if (_handlers.TryGetValue(t, out var l))
                        l.Remove(h);
                }
                _pendingRemove.Clear();
            }
        }

        /// <summary>
        /// Supprime tous les abonnements pour un type donné.
        /// Utile au chargement de scène si nécessaire.
        /// </summary>
        public static void Clear<T>() where T : struct, IGameEvent
        {
            _handlers.Remove(typeof(T));
        }

        /// <summary>
        /// Supprime tous les abonnements. À utiliser avec précaution.
        /// </summary>
        public static void ClearAll()
        {
            _handlers.Clear();
            _pendingRemove.Clear();
        }

        #endregion

        #region Internals

        // Wrapper objet qui permet de retrouver le handler original lors du Unsubscribe
        private class HandlerWrapper<T> where T : struct, IGameEvent
        {
            public readonly Action<T> Original;
            public HandlerWrapper(Action<T> original) { Original = original; }
            public void Invoke(object obj) { Original((T)obj); }
        }

        private static Action<object> WrapHandler<T>(Action<T> handler) where T : struct, IGameEvent
        {
            var wrapper = new HandlerWrapper<T>(handler);
            return wrapper.Invoke;
        }

        #endregion
    }

    /// <summary>
    /// Marqueur : toute struct d'event doit implémenter cette interface.
    /// Permet la contrainte générique et facilite la recherche dans le projet.
    /// </summary>
    public interface IGameEvent { }
}
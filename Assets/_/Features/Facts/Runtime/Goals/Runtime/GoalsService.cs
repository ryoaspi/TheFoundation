using UnityEngine;

namespace TheFundation.Runtime
{
    public static class GoalsService
    {
        private static GoalsCollection _collection;
        public static bool IsInitialized { get; private set; }

        public static void Initialize(GoalsCollection collection)
        {
            if (IsInitialized) return;
            _collection = collection;
            IsInitialized = true;
        }

        // ---------------- PROGRESS ---------------- //

        public static int GetProgress(string key)
        {
            if (GameManager.Facts.TryGetFact(key, out int value))
                return value;

            return 0;
        }

        public static void SetProgress(string key, int value)
        {
            GameManager.Facts.SetFact(key, value, FactDictionary.FactPersistence.Persistent);
        }

        public static void AddProgress(string key, int amount)
        {
            int current = GetProgress(key);
            SetProgress(key, current + amount);
        }

        public static bool IsCompleted(string key)
        {
            return GameManager.Facts.TryGetFact(key + "_completed", out bool v) && v;
        }

        public static void MarkCompleted(string key)
        {
            GameManager.Facts.SetFact(key + "_completed", true, FactDictionary.FactPersistence.Persistent);
        }

        public static void ResetGoal(string key)
        {
            GameManager.Facts.RemoveFact(key);
            GameManager.Facts.RemoveFact(key + "_completed");
        }

        public static void ResetAllGoals()
        {
            foreach (var goal in _collection.m_goals)
            {
                if (goal == null) continue;
                ResetGoal(goal.m_Key);
            }
        }

        public static int GetCurrentStep(string chainKey)
        {
            GameManager.Facts.TryGetFact(chainKey + "_stepIndex", out int index);
            return index;
        }

        public static GoalDefinition GetCurrentStepDefinition(string chainKey, GoalChainDefinition chain)
        {
            int i = GetCurrentStep(chainKey);
            if (i < 0 || i >= chain.m_steps.Length)
                return null;
            return chain.m_steps[i];
        }


        // ---------------- RUNTIME LOGIC ---------------- //

        /// <summary>
        /// Appelé lorsqu'une action de jeu est réussie.
        /// Ex : tuer un ennemi, collecter un objet, parler à un PNJ...
        /// </summary>
        public static void Notify(string key, int amount = 1)
        {
            var def = _collection.Get(key);
            if (def == null) return;

            if (def.m_IsInstantGoal)
            {
                MarkCompleted(key);
                GoalEvents.RaiseGoalCompleted(key);
                return;
            }

            AddProgress(key, amount);
            int current = GetProgress(key);
            
            GoalEvents.RaiseGoalProgress(key, GetProgress(key));

            if (current >= def.m_TargetValue)
            {
                MarkCompleted(key);
                GoalEvents.RaiseGoalCompleted(key);
            }
        }
        
        public static void NotifyChain(string chainKey, GoalChainDefinition chain, string goalKey, int amount = 1)
        {
            var currentStep = GetCurrentStepDefinition(chainKey, chain);
            if (currentStep == null) return;

            // Si ce n’est pas le bon goal pour cette étape
            if (currentStep.m_Key != goalKey)
                return;

            // Mise à jour normale
            Notify(goalKey, amount);

            // Si terminé, avancer
            if (IsCompleted(goalKey))
            {
                int next = GetCurrentStep(chainKey) + 1;
                GameManager.Facts.SetFact(chainKey + "_stepIndex", next,FactDictionary.FactPersistence.Persistent);

                // Si la chaîne est entièrement terminée
                if (next >= chain.m_steps.Length)
                {
                    GameManager.Facts.SetFact(chainKey + "_completed", true, FactDictionary.FactPersistence.Persistent);
                }
            }
        }

    }
}

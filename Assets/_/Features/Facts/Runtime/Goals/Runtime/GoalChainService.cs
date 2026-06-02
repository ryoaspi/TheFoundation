using UnityEngine;

namespace TheFundation.Runtime
{
    public static class GoalChainService
    {
        public static int GetCurrentStep(string chainKey)
        {
            if (GameManager.Facts.TryGetFact(chainKey + "_stepIndex", out int index))
                return index;

            return 0; // première étape
        }

        public static void SetCurrentStep(string chainKey, int step)
        {
            GameManager.Facts.SetFact(
                chainKey + "_stepIndex", 
                step, 
                FactDictionary.FactPersistence.Persistent);
        }

        public static bool IsChainCompleted(string chainKey, GoalChainDefinition chain)
        {
            int current = GetCurrentStep(chainKey);
            return current >= chain.StepCount;
        }

        public static GoalDefinition GetCurrentStepDefinition(string chainKey, GoalChainDefinition chain)
        {
            int i = GetCurrentStep(chainKey);
            if (i < 0 || i >= chain.StepCount)
                return null;

            return chain.m_steps[i];
        }

        /// <summary>
        /// Notifié lorsqu'un objectif lié à la chaîne est rempli.
        /// </summary>
        public static void NotifyChain(string chainKey, GoalChainDefinition chain, string goalKey, int amount = 1)
        {
            if (IsChainCompleted(chainKey, chain))
                return;

            var current = GetCurrentStepDefinition(chainKey, chain);
            if (current == null) return;

            // Ce n’est pas la bonne étape
            if (current.m_Key != goalKey)
                return;

            // On notifie le goal simple
            GoalsService.Notify(goalKey, amount);

            // Si l’étape est complète → on avance dans la chaîne
            if (GoalsService.IsCompleted(goalKey))
            {
                int nextStep = GetCurrentStep(chainKey) + 1;

                SetCurrentStep(chainKey, nextStep);

                // Fin de chaîne
                if (nextStep >= chain.StepCount)
                    GameManager.Facts.SetFact(chainKey + "_completed", true, FactDictionary.FactPersistence.Persistent);
            }
        }
    }
}

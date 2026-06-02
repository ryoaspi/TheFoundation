using System;

namespace TheFundation.Runtime
{
    public static class GoalEvents
    {
        public static event Action<string> OnGoalCompleted;
        public static event Action<string, int> OnGoalProgress;

        internal static void RaiseGoalProgress(string key, int value)
        {
            OnGoalProgress?.Invoke(key, value);
        }

        internal static void RaiseGoalCompleted(string key)
        {
            OnGoalCompleted?.Invoke(key);
        }
    }
}
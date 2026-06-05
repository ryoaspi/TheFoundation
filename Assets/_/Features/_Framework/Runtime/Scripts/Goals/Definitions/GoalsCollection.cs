using UnityEngine;

namespace TheFoundation.Runtime
{
    [CreateAssetMenu(
        fileName = "GoalsCollection",
        menuName = "TheFoundation/Goals/Goal Collection")]
    public class GoalsCollection : ScriptableObject
    {
        public GoalDefinition[] m_goals;

        public GoalDefinition Get(string key)
        {
            foreach (var g in m_goals)
                if (g != null && g.m_Key == key)
                    return g;

            return null;
        }
    }
}
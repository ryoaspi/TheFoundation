using UnityEngine;

namespace TheFundation.Runtime
{
    [CreateAssetMenu(
        fileName = "GoalChainDefinition",
        menuName = "TheFundation/Goals/Goal Chain")]
    public class GoalChainDefinition : ScriptableObject
    {
        public string m_ChainKey;          // ex : quest_forgeMastery
        public GoalDefinition[] m_steps;   // ordre strict

        public int StepCount => m_steps?.Length ?? 0;
    }
}
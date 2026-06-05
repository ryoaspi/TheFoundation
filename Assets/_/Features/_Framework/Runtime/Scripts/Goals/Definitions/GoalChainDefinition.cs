using UnityEngine;

namespace TheFoundation.Runtime
{
    [CreateAssetMenu(
        fileName = "GoalChainDefinition",
        menuName = "TheFoundation/Goals/Goal Chain")]
    public class GoalChainDefinition : ScriptableObject
    {
        public string m_ChainKey;          // ex : quest_forgeMastery
        public GoalDefinition[] m_steps;   // ordre strict

        public int StepCount => m_steps?.Length ?? 0;
    }
}
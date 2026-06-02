using UnityEngine;

namespace TheFundation.Runtime
{
    [CreateAssetMenu(
        fileName = "GoalDefinition",
        menuName = "TheFundation/Goals/Goal Definition")]
    public class GoalDefinition : ScriptableObject
    {
        [Header("Identification")]
        public string m_Key;                 // ex: goal_collectCoins
        public GoalCategory m_Category;

        [Header("UI Display (fallback)")]
        public string m_labelText;
        public string m_descriptionText;

        [Header("Localization Keys (optional)")]
        public string m_LabelKey;           // ex: goals.collectCoins.label
        public string m_DescriptionKey;     // ex: goals.collectCoins.desc
        public string m_CompletedKey;       // ex: goals.collectCoins.completed

        [Header("Goal Settings")]
        public bool m_IsInstantGoal = false; // ex : “Talk to NPC”
        public int m_TargetValue = 1;        // ex : collect 10 coins
        
        [Header("Reward")]
        public RewardType m_RewardType = RewardType.None;
        public double m_rewardValue = 0f;
    }

    public enum RewardType
    {
        None,
        Croquettes,
        Poissons,
        Poulets,
        UpgradePoint,
        PermanentBonus
    }
}
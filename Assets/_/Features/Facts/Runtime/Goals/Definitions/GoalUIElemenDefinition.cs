using UnityEngine;

namespace TheFundation.Runtime
{
    public class GoalUIElemenDefinition : MonoBehaviour
    {
        [CreateAssetMenu(fileName = "GoalUIElemenDefinition", menuName = "TheFundation/ Goals/ UI Element Definition")]
        public class GoalUIElementDefinition : ScriptableObject
        {
            [Header("Goal to Display")] 
            public string m_goalKey;
            
            [Header("Show Progress Bar")]
            public bool m_ShowProgress = true;

            [Header("Visual Option")] 
            public Sprite m_icons;

        }
    }
}

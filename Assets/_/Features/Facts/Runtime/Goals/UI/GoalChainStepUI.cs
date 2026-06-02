using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheFundation.Runtime
{
    public class GoalChainStepUI : FBehaviour
    {
        #region Publics

        [Header("Goal Data")]
        public GoalDefinition m_goal;   // DOIT être vide dans le prefab

        [Header("UI")]
        public TMP_Text m_labelText;
        public TMP_Text m_descriptionText;
        public TMP_Text m_progressText;
        public Slider m_progressBar;

        public GameObject m_iconCompleted;
        public GameObject m_iconCurrent;
        public GameObject m_iconLocked;

        #endregion


        #region Unity API

        protected override void Start()
        {
            base.Start();

            // Empêcher tout Refresh avant assignation correcte
            if (m_goal == null) return;
            if (!GoalsService.IsInitialized) return;

            RefreshUI();
        }

        #endregion


        #region Utils

        public void RefreshUI()
        {
            if (m_goal == null) return;
            if (string.IsNullOrWhiteSpace(m_goal.m_Key)) return;

            // Texte
            if (m_labelText)
                m_labelText.text = LocalizationManager.GetText(m_goal.m_LabelKey);

            if (m_descriptionText)
                m_descriptionText.text = LocalizationManager.GetText(m_goal.m_DescriptionKey);

            // PROGRESSION
            int progress = GoalsService.GetProgress(m_goal.m_Key);
            int target   = m_goal.m_TargetValue;

            if (m_progressText)
                m_progressText.text = $"{progress}/{target}";

            if (m_progressBar)
                m_progressBar.value = target > 0 ? (float)progress / target : 0f;
        }

        #endregion
    }
}
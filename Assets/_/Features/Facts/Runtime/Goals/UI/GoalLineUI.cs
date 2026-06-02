using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheFundation.Runtime
{
    /// <summary>
    /// GoalLineUI
    /// -----------
    /// Affiche un objectif (GoalDefinition) dans l’UI :
    /// - Localisation automatique (label/description)
    /// - Mise à jour automatique via GoalEvents
    /// - Support progression (texte + progressBar)
    /// - Icônes Completed / InProgress
    /// 
    /// Doit être placé sur un prefab UI (hérite de FBehaviour).
    /// </summary>
    public class GoalLineUI : FBehaviour
    {
        #region Publics (Bindings depuis l’Inspector)

        [Header("Goal Source")]
        [Tooltip("Référence vers la GoalsCollection contenant toutes les définitions.")]
        public GoalsCollection m_goalsCollection;

        [Tooltip("Clé du Goal à afficher, ex: goal_collectCoins")]
        public string m_goalKey;

        [Header("UI Références")]
        public TMP_Text m_labelText;
        public TMP_Text m_descriptionText;

        public TMP_Text m_progressText;
        public Slider m_progressBar;

        public GameObject m_completedIcon;
        public GameObject m_inProgressIcon;

        #endregion


        #region Private Fields

        private GoalDefinition _goalDef;

        #endregion


        #region Unity API

        protected override void Start()
        {
            base.Start();

            if (!Validate())
            {
                enabled = false;
                return;
            }

            ApplyLocalization();
            RefreshUI();

            GoalEvents.OnGoalProgress += OnGoalProgress;
            GoalEvents.OnGoalCompleted += OnGoalCompleted;
        }

        private void OnDestroy()
        {
            GoalEvents.OnGoalProgress -= OnGoalProgress;
            GoalEvents.OnGoalCompleted -= OnGoalCompleted;
        }

        #endregion


        #region Utils (Validation)

        private bool Validate()
        {
            if (m_goalsCollection == null)
            {
                Debug.LogError("[GoalLineUI] GoalsCollection manquante.", this);
                return false;
            }

            if (string.IsNullOrEmpty(m_goalKey))
            {
                Debug.LogError("[GoalLineUI] m_goalKey non renseigné.", this);
                return false;
            }

            _goalDef = m_goalsCollection.Get(m_goalKey);
            if (_goalDef == null)
            {
                Debug.LogError($"[GoalLineUI] Goal introuvable dans la collection : {m_goalKey}", this);
                return false;
            }

            return true;
        }

        #endregion


        #region Utils (UI Updates)

        private void ApplyLocalization()
        {
            if (m_labelText)
            {
                if (!string.IsNullOrEmpty(_goalDef.m_LabelKey))
                    m_labelText.text = LocalizationManager.GetText(_goalDef.m_LabelKey);
                else
                    m_labelText.text = _goalDef.m_labelText;
            }

            if (m_descriptionText)
            {
                if (!string.IsNullOrEmpty(_goalDef.m_DescriptionKey))
                    m_descriptionText.text = LocalizationManager.GetText(_goalDef.m_DescriptionKey);
                else
                    m_descriptionText.text = _goalDef.m_descriptionText;
            }
        }

        private void RefreshUI()
        {
            int progress = GoalsService.GetProgress(_goalDef.m_Key);
            bool completed = GoalsService.IsCompleted(_goalDef.m_Key);

            // Icônes Completed / InProgress
            if (m_completedIcon) m_completedIcon.SetActive(completed);
            if (m_inProgressIcon) m_inProgressIcon.SetActive(!completed);

            // Texte de progression
            if (m_progressText)
            {
                if (_goalDef.m_TargetValue > 1)
                    m_progressText.text = $"{progress} / {_goalDef.m_TargetValue}";
                else
                    m_progressText.text = completed ? "✔" : "";
            }

            // Progress bar
            if (m_progressBar && _goalDef.m_TargetValue > 1)
            {
                float ratio = Mathf.Clamp01((float)progress / _goalDef.m_TargetValue);
                m_progressBar.value = ratio;
            }
        }

        #endregion


        #region Event Handlers

        private void OnGoalProgress(string key, int value)
        {
            if (key == _goalDef.m_Key)
                RefreshUI();
        }

        private void OnGoalCompleted(string key)
        {
            if (key == _goalDef.m_Key)
                RefreshUI();
        }

        #endregion

    }
}

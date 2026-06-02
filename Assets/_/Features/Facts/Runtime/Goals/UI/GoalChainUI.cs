using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// GoalChainUI
    /// ------------
    /// Affiche une chaîne complète d’objectifs :
    /// - Nom / Description globales
    /// - Liste des étapes (GoalChainStepUI)
    /// - Icones: Completed / Current / Locked
    /// - Mise à jour en fonction de GoalChainService
    /// </summary>
    public class GoalChainUI : FBehaviour
    {
        #region Publics (Bindings)

        [Header("Source")]
        public GoalChainDefinition m_chain;

        [Header("UI")]
        public TMPro.TMP_Text m_chainLabel;
        public TMPro.TMP_Text m_chainDescription;

        [Header("Prefab Step UI")]
        public GameObject m_stepPrefab;

        [Header("Parent Steps")]
        public Transform m_stepsRoot;

        #endregion


        #region Private

        private int _currentStep;

        #endregion


        #region Unity

        protected override void Start()
        {
            base.Start();
            
            if (!GoalsService.IsInitialized)
            {
                Debug.LogWarning("[GoalChainUI] GoalsService non initialisé, UI GoalChain désactivée jusqu'à init.");
                enabled = false;
                return;
            }

            if (!Validate())
            {
                enabled = false;
                return;
            }

            ApplyLocalization();
            GenerateSteps();
            RefreshChainState();

            GoalEvents.OnGoalCompleted += OnGoalCompleted;
            GoalEvents.OnGoalProgress += OnGoalProgress;
        }

        private void OnDestroy()
        {
            GoalEvents.OnGoalCompleted -= OnGoalCompleted;
            GoalEvents.OnGoalProgress -= OnGoalProgress;
        }

        #endregion


        #region Validation

        private bool Validate()
        {
            if (m_chain == null)
            {
                Debug.LogError("[GoalChainUI] m_chain non assigné.", this);
                return false;
            }

            if (m_stepPrefab == null)
            {
                Debug.LogError("[GoalChainUI] m_stepPrefab non assigné.", this);
                return false;
            }

            if (m_stepsRoot == null)
            {
                Debug.LogError("[GoalChainUI] m_stepsRoot non assigné.", this);
                return false;
            }

            return true;
        }

        #endregion


        #region UI Build

        private void ApplyLocalization()
        {
            if (m_chainLabel)
                m_chainLabel.text = m_chain.m_ChainKey; // Ou via localisation plus tard

            if (m_chainDescription)
                m_chainDescription.text = ""; // Option future : description globale
        }


        private void GenerateSteps()
        {
            foreach (Transform c in m_stepsRoot)
                Destroy(c.gameObject);

            foreach (var step in m_chain.m_steps)
            {
                if (step == null) continue;

                GameObject inst = Instantiate(m_stepPrefab, m_stepsRoot);

                var ui = inst.GetComponent<GoalChainStepUI>();
                ui.m_goal = step;

                ui.RefreshUI(); // initialize
            }
        }

        #endregion


        #region Refresh Logic

        private void RefreshChainState()
        {
            _currentStep = GoalChainService.GetCurrentStep(m_chain.m_ChainKey);

            int index = 0;
            foreach (Transform child in m_stepsRoot)
            {
                var ui = child.GetComponent<GoalChainStepUI>();

                if (ui == null) continue;

                bool completed = index < _currentStep;
                bool current = index == _currentStep;
                bool locked = index > _currentStep;

                if (ui.m_iconCompleted) ui.m_iconCompleted.SetActive(completed);
                if (ui.m_iconCurrent) ui.m_iconCurrent.SetActive(current);
                if (ui.m_iconLocked) ui.m_iconLocked.SetActive(locked);

                index++;
            }
        }

        #endregion


        #region Events

        private void OnGoalProgress(string key, int value)
        {
            RefreshChainState();
        }

        private void OnGoalCompleted(string key)
        {
            RefreshChainState();
        }

        #endregion
    }
}

using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// GoalPanelUI
    /// -----------
    /// Génère automatiquement un ensemble de lignes d’objectifs (GoalLineUI)
    /// à partir d’une GoalsCollection.
    /// 
    /// Principe :
    /// - On assigne GoalsCollection dans l’inspector
    /// - On assigne un prefab GoalLineUI
    /// - Le panel instancie et configure automatiquement chaque ligne
    /// </summary>
    public class GoalPanelUI : FBehaviour
    {
        #region Publics (UI Bindings)

        [Header("Sources")]
        [Tooltip("Liste complète des objectifs du jeu.")]
        public GoalsCollection m_goalsCollection;

        [Header("Prefab")]
        [Tooltip("Prefab contenant un GoalLineUI (hérite de FBehaviour).")]
        public GameObject m_goalLinePrefab;

        [Header("Parent pour les éléments UI instanciés")]
        public Transform m_contentRoot;

        #endregion


        #region Private 

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

            GenerateUI();
        }

        #endregion


        #region Main Methods

        private void GenerateUI()
        {
            foreach (var goal in m_goalsCollection.m_goals)
            {
                if (goal == null) continue;

                GameObject instance = Instantiate(m_goalLinePrefab, m_contentRoot);

                // On récupère le GoalLineUI (obligatoire dans ton prefab)
                var lineUI = instance.GetComponent<GoalLineUI>();

                if (lineUI == null)
                {
                    Debug.LogError("[GoalPanelUI] Le prefab ne contient pas GoalLineUI.");
                    continue;
                }

                // On configure la ligne
                lineUI.m_goalsCollection = m_goalsCollection;
                lineUI.m_goalKey = goal.m_Key;
            }
        }

        #endregion


        #region Validation

        private bool Validate()
        {
            if (m_goalsCollection == null)
            {
                Debug.LogError("[GoalPanelUI] GoalsCollection non assignée.", this);
                return false;
            }

            if (m_goalLinePrefab == null)
            {
                Debug.LogError("[GoalPanelUI] Prefab GoalLineUI manquant.", this);
                return false;
            }

            if (m_contentRoot == null)
            {
                Debug.LogError("[GoalPanelUI] Aucun parent m_contentRoot assigné.", this);
                return false;
            }

            return true;
        }

        #endregion

    }
}

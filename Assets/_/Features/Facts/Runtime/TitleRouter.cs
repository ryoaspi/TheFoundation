using UnityEngine;

namespace TheFundation.Runtime
{
    public class TitleRouter : MonoBehaviour
    {
        #region Unity Api

        void Start()
        {
            // 1) Détermine si première ouverture
            bool haslaunchedBefore = false;
            GameManager.Facts.TryGetFact("has_launched_before", out haslaunchedBefore);
            
            // 2) Vérifie s'il existe AU MOINS une sauvegarde
            bool hasSave = GameManager.AnySaveExists();
            
            // Option : synchroniser un fact << has_save >>
            GameManager.Facts.SetFact("has_save", hasSave, FactDictionary.FactPersistence.Normal);
            
            // 3) Activer le bon panneau
            SetAll(false);
            if (!haslaunchedBefore) _firstLaunchPanel?.SetActive(true);
            else if (hasSave) _continuePanel?.SetActive(true);
            else _defaultPanel?.SetActive(true);
            
            // 4) Marque l'appli comme déjà lancée (après affichage du 1er écran)
            if (!haslaunchedBefore) GameManager.Facts.SetFact("has_launched_before", true, FactDictionary.FactPersistence.Persistent);
        }

        #endregion


        #region Main Methods
        
        private void SetAll(bool state)
        {
            if (_firstLaunchPanel) _firstLaunchPanel.SetActive(state);
            if (_continuePanel) _continuePanel.SetActive(state);
            if (_defaultPanel) _defaultPanel.SetActive(state);
        }

        #endregion
        
        
        
        #region Private And Protected

        [Header("Panels")]
        [SerializeField] private GameObject _firstLaunchPanel; // << Nouveau joueur >>
        [SerializeField] private GameObject _continuePanel; // << Continuer >>
        [SerializeField] private GameObject _defaultPanel; // << Jouer >>
        
        [Header("Save Slots")]
        [SerializeField] private int _maxSlot = 10;
        

        #endregion
    }
}

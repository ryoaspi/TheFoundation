using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheFundation.Runtime
{
    public class TitleAction : MonoBehaviour
    {
        #region Utils

        public void NewGame()
        {
           
            // Marque le premier lancement + pas de save encore (si tu veux forcer)
            GameManager.Facts.SetFact("has_launched_before", true, FactDictionary.FactPersistence.Persistent);
            
            // Charge la scène de jeu
            if (!string.IsNullOrEmpty(_gameplaySceneName))
                SceneManager.LoadScene(_gameplaySceneName);
        }

        public void ContinueGame()
        {
            int slot = FirstExistingSlot();
            if (slot >= 0)
            {
                GameManager.LoadFromSlot(slot);
                if (!string.IsNullOrEmpty(_gameplaySceneName))
                    SceneManager.LoadScene(_gameplaySceneName);
            }
            else
            {
                Debug.LogWarning("[TitleActions] Aucun slot disponible pour Continuer.");
            }
        }
        
        // Bouton : Quitter
        public void QuitGame()
        {
            Application.Quit();
        }

        public int FirstExistingSlot()
        {
            for (int i = 0; i < GameManager._MaxSlots; i++)
                if (GameManager.HasSaveInSlot(i)) return i;
            return -1;
        }

        #endregion
        
        
        #region Private And Protected

        [Header("Scene to load for New/Continue")] 
        [SerializeField] private string _gameplaySceneName = "Game"; // change le nom selon le projet

        #endregion
    }
}

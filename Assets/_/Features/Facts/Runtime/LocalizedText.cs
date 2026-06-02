using TMPro;
using UnityEngine;

namespace TheFundation.Runtime
{
    [RequireComponent(typeof(TMP_Text))]
    // On hérite de FBehaviour pour profiter de l'abonnement auto à OnLanguageChanged
    public class LocalizedText : FBehaviour 
    {
        [SerializeField] private string key;
        private TMP_Text _text;

        protected override void Start()
        {
            base.Start(); // Important pour AutoBindSettingIfNeeded
            _text = GetComponent<TMP_Text>();
            UpdateText();
        }

        // On override la méthode prévue dans FBehaviour
        protected override void OnLocalizationChanged()
        {
            UpdateText();
        }

        public void SetKey(string k) 
        { 
            key = k; 
            UpdateText(); 
        }

        public void SetFormattedText(params object[] args)
        {
            if (string.IsNullOrEmpty(key) || !_text) return;
            // Utilise la méthode L() de FBehaviour qui gère déjà les fallbacks
            string localizedPattern = L(key);
            _text.text = string.Format(localizedPattern, args);
        }

        private void UpdateText()
        {
            if (_text && !string.IsNullOrEmpty(key))
            {
                // On utilise L() pour avoir les [crochets] si la clé manque
                _text.text = L(key);
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TheFundation.Runtime
{
    public class LanguageDropdownBridge : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;
    
        // La liste de tes langues (tu peux la remplir dans l'inspecteur)
        [SerializeField] private List<LanguageEntry> _languages = new List<LanguageEntry>();

        void Start()
        {
            if (_dropdown == null) _dropdown = GetComponent<TMP_Dropdown>();
        
            SetupDropdown();
        }

        private void SetupDropdown()
        {
            _dropdown.ClearOptions();

            // On crée les options à partir de notre liste modulaire
            List<string> options = _languages.Select(x => x.label).ToList();
            _dropdown.AddOptions(options);

            // On sélectionne la langue actuellement sauvegardée
            string current = LocalizationManager.CurrentLanguage;
            int index = _languages.FindIndex(x => x.langCode == current);
            if (index != -1) _dropdown.SetValueWithoutNotify(index);

            // On écoute le changement
            _dropdown.onValueChanged.AddListener(OnSelected);
        }

        private void OnSelected(int index)
        {
            if (index < 0 || index >= _languages.Count) return;

            string code = _languages[index].langCode;
            Debug.Log($"[Language] Changement vers : {code}");
        
            LocalizationManager.SetLanguage(code);
        }
    }
    
    [System.Serializable]
    public class LanguageEntry
    {
        public string label;      // Ce qui sera écrit dans le Dropdown (ex: "Français")
        public string langCode;   // Le nom du fichier JSON (ex: "fr")
    }
}

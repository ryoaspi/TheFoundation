using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheFoundation.Runtime
{
    /// <summary>
    /// KanaInputField — IME romaji → kana pour TMP_InputField
    /// --------------------------------------------------------
    /// Intercepte la saisie clavier et la convertit en kana
    /// en temps réel, sans IME Windows.
    ///
    /// Fonctionnement :
    ///   Chaque frame, Input.inputString est consommé caractère par caractère.
    ///   Un buffer accumule les lettres en cours (ex: "sh").
    ///   Dès qu'une séquence complète est détectée (ex: "sha"), le kana
    ///   est injecté dans le TMP_InputField et le buffer est vidé.
    ///
    /// Mode kana :
    ///   - Hiragana par défaut
    ///   - Toggle via SetMode() ou bouton optionnel dans l'Inspector
    ///   - Raccourci clavier configurable (défaut : F7 = hiragana, F8 = katakana)
    ///
    /// Zéro coroutine, zéro Task, zéro async.
    ///
    /// Setup :
    ///   1. Ajouter ce composant sur le même GameObject que TMP_InputField
    ///      (ou assigner m_targetField depuis l'Inspector).
    ///   2. Optionnel : assigner m_modeToggleButton et m_modeLabel.
    ///   3. Optionnel : assigner m_pendingLabel pour afficher le buffer en cours.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class KanaInputField : FBehaviour
    {
        #region Inspector

        [Header("Champ cible (auto-détecté si sur le même GO)")]
        [SerializeField] private TMP_InputField m_targetField;

        [Header("Mode kana initial")]
        [SerializeField] private KanaConverter.KanaMode m_initialMode = KanaConverter.KanaMode.Hiragana;

        [Header("UI optionnelle")]
        [Tooltip("Bouton qui toggle entre hiragana et katakana.")]
        [SerializeField] private Button m_modeToggleButton;

        [Tooltip("Label affichant le mode actuel (ex: 'あ' ou 'ア').")]
        [SerializeField] private TMP_Text m_modeLabel;

        [Tooltip("Label affichant le buffer en attente (séquence incomplète).")]
        [SerializeField] private TMP_Text m_pendingLabel;

        [Header("Raccourcis clavier")]
        [Tooltip("Touche pour forcer le mode hiragana.")]
        [SerializeField] private KeyCode m_hiraganaKey = KeyCode.F7;

        [Tooltip("Touche pour forcer le mode katakana.")]
        [SerializeField] private KeyCode m_katakanaKey = KeyCode.F8;

        [Tooltip("Si true, 'n' suivi d'une non-voyelle commit ん automatiquement.")]
        [SerializeField] private bool m_autoCommitN = true;

        #endregion

        #region Publics

        /// <summary>Mode kana actuellement actif.</summary>
        public KanaConverter.KanaMode CurrentMode { get; private set; }

        /// <summary>Buffer de saisie en cours (séquence incomplète).</summary>
        public string PendingBuffer => _buffer;

        /// <summary>
        /// Change le mode kana programmatiquement.
        /// </summary>
        public void SetMode(KanaConverter.KanaMode mode)
        {
            CurrentMode = mode;
            FlushBuffer(); // vider le buffer pour éviter des séquences hybrides
            RefreshModeUI();
        }

        /// <summary>Bascule entre hiragana et katakana.</summary>
        public void ToggleMode()
        {
            SetMode(CurrentMode == KanaConverter.KanaMode.Hiragana
                ? KanaConverter.KanaMode.Katakana
                : KanaConverter.KanaMode.Hiragana);
        }

        /// <summary>Vide le buffer sans rien injecter.</summary>
        public void ClearBuffer()
        {
            _buffer = string.Empty;
            RefreshPendingUI();
        }

        /// <summary>
        /// Vide le champ de saisie et le buffer.
        /// </summary>
        public void ClearAll()
        {
            ClearBuffer();
            if (m_targetField)
                m_targetField.text = string.Empty;
        }

        /// <summary>Texte actuellement dans le champ (kana commités).</summary>
        public string GetText() => m_targetField ? m_targetField.text : string.Empty;

        #endregion

        #region Unity lifecycle

        protected override void Start()
        {
            base.Start();

            // Auto-détection du TMP_InputField
            if (!m_targetField)
                m_targetField = GetComponent<TMP_InputField>();

            if (!m_targetField)
            {
                Error("Aucun TMP_InputField trouvé. Assigne m_targetField dans l'Inspector.");
                enabled = false;
                return;
            }

            CurrentMode = m_initialMode;

            // Désactiver l'IME natif Unity sur ce champ pour éviter les conflits
            Input.imeCompositionMode = IMECompositionMode.Off;

            // Bloquer la saisie directe du TMP_InputField
            // (on gère tout nous-mêmes via Input.inputString)
            m_targetField.onValidateInput += BlockDirectInput;

            // Bouton toggle optionnel
            if (m_modeToggleButton)
                m_modeToggleButton.onClick.AddListener(ToggleMode);

            RefreshModeUI();
            RefreshPendingUI();
        }

        private void OnDestroy()
        {
            if (m_targetField)
                m_targetField.onValidateInput -= BlockDirectInput;

            if (m_modeToggleButton)
                m_modeToggleButton.onClick.RemoveListener(ToggleMode);

            // Restaurer l'IME natif
            Input.imeCompositionMode = IMECompositionMode.Auto;
        }

        private void Update()
        {
            if (!m_targetField || !m_targetField.isFocused)
                return;

            // ── Raccourcis mode ─────────────────────────────────
            if (Input.GetKeyDown(m_hiraganaKey))
            {
                SetMode(KanaConverter.KanaMode.Hiragana);
                return;
            }
            if (Input.GetKeyDown(m_katakanaKey))
            {
                SetMode(KanaConverter.KanaMode.Katakana);
                return;
            }

            // ── Backspace ────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                HandleBackspace();
                return;
            }

            // ── Saisie caractère par caractère ───────────────────
            foreach (char c in Input.inputString)
            {
                // Ignorer les caractères de contrôle (Backspace géré au-dessus)
                if (c == '\b' || c == '\r' || c == '\n' || c < ' ')
                    continue;

                ProcessChar(c);
            }
        }

        #endregion

        #region Automate IME

        private string _buffer = string.Empty;

        /// <summary>
        /// Traite un caractère entrant dans l'automate.
        /// </summary>
        private void ProcessChar(char c)
        {
            string lower = char.ToLowerInvariant(c).ToString();

            // Cas spécial : 'n' suivi d'une non-voyelle ou d'un autre 'n'
            // → commit ん/ン immédiatement
            if (m_autoCommitN && _buffer == "n")
            {
                if (lower != "a" && lower != "i" && lower != "u" &&
                    lower != "e" && lower != "o" && lower != "y")
                {
                    CommitKana(CurrentMode == KanaConverter.KanaMode.Hiragana ? "ん" : "ン");
                    // Ne pas return : continuer avec le nouveau caractère
                }
            }

            _buffer += lower;

            // ── Tentative de commit ──────────────────────────────
            if (KanaConverter.TryConsume(_buffer, CurrentMode,
                out string kana, out string remaining))
            {
                CommitKana(kana);

                // S'il reste quelque chose (ex: "kka" → っ + "ka" en buffer)
                _buffer = remaining;

                // Le remaining peut lui-même être une séquence complète
                // (rare mais possible avec les consonnes doublées)
                if (!string.IsNullOrEmpty(_buffer) &&
                    KanaConverter.TryConsume(_buffer, CurrentMode, out string kana2, out string rem2))
                {
                    CommitKana(kana2);
                    _buffer = rem2;
                }
            }
            else if (!KanaConverter.IsPending(_buffer))
            {
                // Séquence invalide : flush le buffer brut sauf le dernier char
                // (le dernier char pourrait commencer une nouvelle séquence valide)
                string flushed = _buffer.Substring(0, _buffer.Length - 1);
                string lastChar = _buffer.Substring(_buffer.Length - 1);

                if (!string.IsNullOrEmpty(flushed))
                    CommitRaw(flushed);

                // Recommencer avec le dernier caractère
                _buffer = string.Empty;
                ProcessChar(c); // récursion sur le dernier char uniquement
                return;
            }

            RefreshPendingUI();
        }

        /// <summary>
        /// Gestion du Backspace :
        ///   - Si buffer non vide → supprimer le dernier char du buffer
        ///   - Si buffer vide → supprimer le dernier kana commité
        /// </summary>
        private void HandleBackspace()
        {
            if (_buffer.Length > 0)
            {
                _buffer = _buffer.Substring(0, _buffer.Length - 1);
                RefreshPendingUI();
            }
            else if (m_targetField && m_targetField.text.Length > 0)
            {
                // Supprimer le dernier caractère commité
                string text = m_targetField.text;
                m_targetField.text = text.Substring(0, text.Length - 1);
                m_targetField.caretPosition = m_targetField.text.Length;
            }
        }

        /// <summary>Flush le buffer tel quel dans le champ (caractères bruts).</summary>
        private void FlushBuffer()
        {
            if (!string.IsNullOrEmpty(_buffer))
            {
                CommitRaw(_buffer);
                _buffer = string.Empty;
                RefreshPendingUI();
            }
        }

        /// <summary>Injecte un kana converti dans le TMP_InputField.</summary>
        private void CommitKana(string kana)
        {
            if (!m_targetField) return;
            m_targetField.text += kana;
            m_targetField.caretPosition = m_targetField.text.Length;
        }

        /// <summary>Injecte du texte brut (non converti) dans le TMP_InputField.</summary>
        private void CommitRaw(string text)
        {
            if (!m_targetField) return;
            m_targetField.text += text;
            m_targetField.caretPosition = m_targetField.text.Length;
        }

        /// <summary>
        /// Callback onValidateInput : bloque toute saisie directe dans le TMP_InputField.
        /// La saisie est entièrement gérée par notre Update().
        /// </summary>
        private char BlockDirectInput(string text, int charIndex, char addedChar)
        {
            return '\0'; // caractère nul = saisie bloquée
        }

        #endregion

        #region UI

        private void RefreshModeUI()
        {
            if (!m_modeLabel) return;
            m_modeLabel.text = CurrentMode == KanaConverter.KanaMode.Hiragana
                ? "あ" // indicateur hiragana
                : "ア"; // indicateur katakana
        }

        private void RefreshPendingUI()
        {
            if (!m_pendingLabel) return;
            m_pendingLabel.text = _buffer; // affiche la séquence en attente
        }

        #endregion
    }
}
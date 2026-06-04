using System.Collections.Generic;

namespace TheFoundation.Runtime
{
    /// <summary>
    /// KanaConverter — moteur de conversion romaji → kana
    /// ----------------------------------------------------
    /// Service statique pur. Aucune dépendance UnityEngine.
    /// Peut être testé hors Unity (tests unitaires, outils éditeur).
    ///
    /// Supporte :
    ///   - Hiragana  (あいうえお…)
    ///   - Katakana  (アイウエオ…)
    ///   - Toutes les combinaisons wāpuro standard (sha, chi, tsu, nn…)
    ///   - Consonnes doublées → petit tsu (kk→っk, ss→っs…)
    ///   - Petits kana combinés (kya, sha, tchi…)
    ///
    /// Usage :
    ///   string kana = KanaConverter.Convert("nihongo", KanaMode.Hiragana);
    ///   // → "にほんご"
    ///
    ///   bool ok = KanaConverter.TryConsume("ka", KanaMode.Hiragana, out string result, out string remaining);
    ///   // result = "か", remaining = ""
    ///
    ///   bool pending = KanaConverter.IsPending("sh");
    ///   // → true  (séquence incomplète, attendre la suite)
    /// </summary>
    public static class KanaConverter
    {
        #region Publics

        public enum KanaMode
        {
            Hiragana,
            Katakana
        }

        /// <summary>
        /// Convertit une chaîne romaji complète en kana.
        /// Les caractères non reconnus sont conservés tels quels.
        /// </summary>
        public static string Convert(string romaji, KanaMode mode)
        {
            if (string.IsNullOrEmpty(romaji))
                return string.Empty;

            var result = new System.Text.StringBuilder();
            string buffer = romaji.ToLowerInvariant();

            int i = 0;
            while (i < buffer.Length)
            {
                bool matched = false;

                // Essayer les séquences les plus longues en premier (max 4 chars : tchi, ltsu...)
                for (int len = System.Math.Min(4, buffer.Length - i); len >= 1; len--)
                {
                    string chunk = buffer.Substring(i, len);

                    // Consonne doublée → petit tsu + suite
                    if (len >= 2 && chunk[0] == chunk[1] && chunk[0] != 'n' && IsConsonant(chunk[0]))
                    {
                        result.Append(mode == KanaMode.Hiragana ? "っ" : "ッ");
                        i++;
                        matched = true;
                        break;
                    }

                    if (TryGetKana(chunk, mode, out string kana))
                    {
                        result.Append(kana);
                        i += len;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    // Caractère non reconnu → on le conserve brut
                    result.Append(buffer[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Tente de consommer une séquence depuis le buffer de l'automate IME.
        /// Retourne true si une conversion complète a été trouvée.
        ///
        /// out result   : le kana produit (ex: "か")
        /// out remaining: ce qui reste dans le buffer après la séquence
        ///                (ex: "kka" → consomme "k" petit-tsu, remaining = "ka")
        /// </summary>
        public static bool TryConsume(string buffer, KanaMode mode,
            out string result, out string remaining)
        {
            result    = string.Empty;
            remaining = string.Empty;

            if (string.IsNullOrEmpty(buffer))
                return false;

            string lower = buffer.ToLowerInvariant();

            // Consonne doublée (kk, ss, tt…) → petit tsu, remaining = reste
            if (lower.Length >= 2 &&
                lower[0] == lower[1] &&
                lower[0] != 'n' &&
                IsConsonant(lower[0]))
            {
                result    = mode == KanaMode.Hiragana ? "っ" : "ッ";
                remaining = lower.Substring(1); // garde la 2e consonne pour la suite
                return true;
            }

            // Recherche séquence la plus longue
            for (int len = System.Math.Min(4, lower.Length); len >= 1; len--)
            {
                string chunk = lower.Substring(0, len);
                if (TryGetKana(chunk, mode, out string kana))
                {
                    result    = kana;
                    remaining = lower.Substring(len);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retourne true si le buffer est une séquence incomplète mais valide
        /// (ex: "k", "sh", "ky" → on attend la suite).
        /// Retourne false si le buffer ne peut mener à aucune séquence connue.
        /// </summary>
        public static bool IsPending(string buffer)
        {
            if (string.IsNullOrEmpty(buffer))
                return false;

            string lower = buffer.ToLowerInvariant();

            // Consonne double en cours (ex: "k" seul, avant "ka")
            if (lower.Length == 1 && IsConsonant(lower[0]) && lower[0] != 'n')
                return true;

            // Vérifier si au moins une entrée de la table commence par ce buffer
            foreach (var key in _hiragana.Keys)
            {
                if (key.StartsWith(lower) && key.Length > lower.Length)
                    return true;
            }

            // "n" seul est pending (peut devenir "nn" → ん ou "na/ni/nu/ne/no")
            if (lower == "n")
                return true;

            return false;
        }

        /// <summary>
        /// Retourne true si le buffer correspond exactement à une séquence reconnue.
        /// </summary>
        public static bool IsComplete(string buffer, KanaMode mode)
        {
            if (string.IsNullOrEmpty(buffer)) return false;
            string lower = buffer.ToLowerInvariant();

            if (lower.Length >= 2 && lower[0] == lower[1] && lower[0] != 'n' && IsConsonant(lower[0]))
                return true;

            return TryGetKana(lower, mode, out _);
        }

        #endregion

        #region Table de conversion

        private static bool TryGetKana(string romaji, KanaMode mode, out string kana)
        {
            var table = mode == KanaMode.Hiragana ? _hiragana : _katakana;
            return table.TryGetValue(romaji, out kana);
        }

        private static bool IsConsonant(char c)
        {
            return "bcdfghjklmnpqrstvwxyz".IndexOf(c) >= 0;
        }

        // ─── Hiragana ─────────────────────────────────────────────

        private static readonly Dictionary<string, string> _hiragana = new()
        {
            // Voyelles
            { "a",   "あ" }, { "i",   "い" }, { "u",   "う" },
            { "e",   "え" }, { "o",   "お" },

            // K
            { "ka",  "か" }, { "ki",  "き" }, { "ku",  "く" },
            { "ke",  "け" }, { "ko",  "こ" },
            { "kya", "きゃ" }, { "kyi", "きぃ" }, { "kyu", "きゅ" },
            { "kye", "きぇ" }, { "kyo", "きょ" },

            // S
            { "sa",  "さ" }, { "si",  "し" }, { "shi", "し" },
            { "su",  "す" }, { "se",  "せ" }, { "so",  "そ" },
            { "sha", "しゃ" }, { "shi", "し" }, { "shu", "しゅ" },
            { "she", "しぇ" }, { "sho", "しょ" },
            { "sya", "しゃ" }, { "syu", "しゅ" }, { "syo", "しょ" },

            // T
            { "ta",  "た" }, { "ti",  "ち" }, { "chi", "ち" },
            { "tu",  "つ" }, { "tsu", "つ" }, { "te",  "て" }, { "to",  "と" },
            { "cha", "ちゃ" }, { "chi", "ち" }, { "chu", "ちゅ" },
            { "che", "ちぇ" }, { "cho", "ちょ" },
            { "tya", "ちゃ" }, { "tyi", "ちぃ" }, { "tyu", "ちゅ" },
            { "tye", "ちぇ" }, { "tyo", "ちょ" },
            { "tchi","っち" },
            { "ltsu","っつ" },

            // N
            { "na",  "な" }, { "ni",  "に" }, { "nu",  "ぬ" },
            { "ne",  "ね" }, { "no",  "の" },
            { "nn",  "ん" }, { "n'",  "ん" },
            { "nya", "にゃ" }, { "nyi", "にぃ" }, { "nyu", "にゅ" },
            { "nye", "にぇ" }, { "nyo", "にょ" },

            // H
            { "ha",  "は" }, { "hi",  "ひ" }, { "hu",  "ふ" },
            { "fu",  "ふ" }, { "he",  "へ" }, { "ho",  "ほ" },
            { "hya", "ひゃ" }, { "hyi", "ひぃ" }, { "hyu", "ひゅ" },
            { "hye", "ひぇ" }, { "hyo", "ひょ" },

            // M
            { "ma",  "ま" }, { "mi",  "み" }, { "mu",  "む" },
            { "me",  "め" }, { "mo",  "も" },
            { "mya", "みゃ" }, { "myi", "みぃ" }, { "myu", "みゅ" },
            { "mye", "みぇ" }, { "myo", "みょ" },

            // Y
            { "ya",  "や" }, { "yu",  "ゆ" }, { "yo",  "よ" },
            { "yi",  "い" },

            // R
            { "ra",  "ら" }, { "ri",  "り" }, { "ru",  "る" },
            { "re",  "れ" }, { "ro",  "ろ" },
            { "rya", "りゃ" }, { "ryi", "りぃ" }, { "ryu", "りゅ" },
            { "rye", "りぇ" }, { "ryo", "りょ" },

            // W
            { "wa",  "わ" }, { "wi",  "ゐ" }, { "we",  "ゑ" }, { "wo",  "を" },

            // G
            { "ga",  "が" }, { "gi",  "ぎ" }, { "gu",  "ぐ" },
            { "ge",  "げ" }, { "go",  "ご" },
            { "gya", "ぎゃ" }, { "gyi", "ぎぃ" }, { "gyu", "ぎゅ" },
            { "gye", "ぎぇ" }, { "gyo", "ぎょ" },

            // Z
            { "za",  "ざ" }, { "zi",  "じ" }, { "ji",  "じ" },
            { "zu",  "ず" }, { "ze",  "ぜ" }, { "zo",  "ぞ" },
            { "ja",  "じゃ" }, { "ju",  "じゅ" }, { "je",  "じぇ" }, { "jo",  "じょ" },
            { "jya", "じゃ" }, { "jyi", "じぃ" }, { "jyu", "じゅ" },
            { "jye", "じぇ" }, { "jyo", "じょ" },
            { "zya", "じゃ" }, { "zyu", "じゅ" }, { "zyo", "じょ" },

            // D
            { "da",  "だ" }, { "di",  "ぢ" }, { "du",  "づ" },
            { "de",  "で" }, { "do",  "ど" },
            { "dya", "ぢゃ" }, { "dyi", "ぢぃ" }, { "dyu", "ぢゅ" },
            { "dye", "ぢぇ" }, { "dyo", "ぢょ" },

            // B
            { "ba",  "ば" }, { "bi",  "び" }, { "bu",  "ぶ" },
            { "be",  "べ" }, { "bo",  "ぼ" },
            { "bya", "びゃ" }, { "byi", "びぃ" }, { "byu", "びゅ" },
            { "bye", "びぇ" }, { "byo", "びょ" },

            // P
            { "pa",  "ぱ" }, { "pi",  "ぴ" }, { "pu",  "ぷ" },
            { "pe",  "ぺ" }, { "po",  "ぽ" },
            { "pya", "ぴゃ" }, { "pyi", "ぴぃ" }, { "pyu", "ぴゅ" },
            { "pye", "ぴぇ" }, { "pyo", "ぴょ" },

            // F (emprunts)
            { "fa",  "ふぁ" }, { "fi",  "ふぃ" }, { "fe",  "ふぇ" }, { "fo",  "ふぉ" },

            // V (emprunts)
            { "va",  "ヴぁ" }, { "vi",  "ヴぃ" }, { "vu",  "ヴ" },
            { "ve",  "ヴぇ" }, { "vo",  "ヴぉ" },

            // Petits kana seuls (préfixe l ou x)
            { "la",  "ぁ" }, { "li",  "ぃ" }, { "lu",  "ぅ" },
            { "le",  "ぇ" }, { "lo",  "ぉ" },
            { "lya", "ゃ" }, { "lyu", "ゅ" }, { "lyo", "ょ" },
            { "ltu", "っ" }, { "ltsu","っ" },
            { "xa",  "ぁ" }, { "xi",  "ぃ" }, { "xu",  "ぅ" },
            { "xe",  "ぇ" }, { "xo",  "ぉ" },
            { "xya", "ゃ" }, { "xyu", "ゅ" }, { "xyo", "ょ" },
            { "xtu", "っ" },

            // Ponctuation japonaise
            { ".",   "。" }, { ",",   "、" },
            { "!",   "！" }, { "?",   "？" },
            { " ",   "　" }, // espace insécable japonais
        };

        // ─── Katakana ─────────────────────────────────────────────
        // Générée depuis la hiragana en décalant l'offset Unicode (+96)
        // Les katakana sont exactement 0x60 (96) au-dessus des hiragana.

        private static readonly Dictionary<string, string> _katakana;

        static KanaConverter()
        {
            _katakana = new Dictionary<string, string>(_hiragana.Count);

            foreach (var kv in _hiragana)
            {
                // Convertir chaque caractère hiragana en katakana
                var sb = new System.Text.StringBuilder(kv.Value.Length);
                foreach (char c in kv.Value)
                {
                    // Hiragana : U+3041–U+3096 → Katakana : U+30A1–U+30F6
                    if (c >= '\u3041' && c <= '\u3096')
                        sb.Append((char)(c + 0x60));
                    else
                        sb.Append(c); // ponctuation, petits kana hors plage → inchangé
                }
                // Ne pas écraser si déjà présent (doublons de table comme shi/si)
                if (!_katakana.ContainsKey(kv.Key))
                    _katakana[kv.Key] = sb.ToString();
            }

            // Surcharges spécifiques katakana (emprunts, sons étrangers)
            _katakana["va"]  = "ヴァ";
            _katakana["vi"]  = "ヴィ";
            _katakana["vu"]  = "ヴ";
            _katakana["ve"]  = "ヴェ";
            _katakana["vo"]  = "ヴォ";
            _katakana["fa"]  = "ファ";
            _katakana["fi"]  = "フィ";
            _katakana["fe"]  = "フェ";
            _katakana["fo"]  = "フォ";
            _katakana["wi"]  = "ウィ";
            _katakana["we"]  = "ウェ";
            _katakana["wo"]  = "ウォ";
            _katakana["-"]   = "ー"; // tiret long katakana
        }

        #endregion
    }
}
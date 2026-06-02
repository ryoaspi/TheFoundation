using System.Text;
using UnityEngine;

namespace TheFundation.Runtime
{
    /// <summary>
    /// GameHealthCheck
    /// ---------------
    /// Petit rapport de debug au démarrage du jeu.
    /// Ne dépend que de :
    /// - GameManager.Facts
    /// - GameManager._MaxSlots
    /// - FactSaveSystem.SlotExist
    /// - Fact "language"
    /// Pas de Goals, pas de Settings, pas de V2.
    /// </summary>
    public static class GameHealthCheck
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void GenerateOnStart()
        {
#if UNITY_EDITOR
            GenerateAndLog();
#endif
        }

        public static void GenerateAndLog()
        {
            var sb = new StringBuilder();

            sb.AppendLine("========== [TheFundation] Game Health Report ==========");
            sb.AppendLine($"Product   : {Application.productName}");
            sb.AppendLine($"Version   : {Application.version}");
            sb.AppendLine($"Platform  : {Application.platform}");
            sb.AppendLine();

            AppendFactsInfo(sb);
            AppendSaveSlotsInfo(sb);
            AppendLanguageInfo(sb);

            sb.AppendLine("=======================================================");
            
        }

        // -------- FACTS ----------
        private static void AppendFactsInfo(StringBuilder sb)
        {
            var facts = GameManager.Facts;              // propriété statique actuelle
            int totalFacts = facts.m_allFacts.Count;    // m_AllFacts existe dans ton FactDictionary
            int persistentFacts = 0;

            foreach (var kvp in facts.m_allFacts)
            {
                if (kvp.Value.IsPersistent)
                    persistentFacts++;
            }

            sb.AppendLine("[Facts]");
            sb.AppendLine($"- Total facts      : {totalFacts}");
            sb.AppendLine($"- Persistent facts : {persistentFacts}");
            sb.AppendLine();
        }

        // -------- SAVE SLOTS ----------
        private static void AppendSaveSlotsInfo(StringBuilder sb)
        {
            sb.AppendLine("[Save Slots]");

            int used = 0;
            var usedIds = new System.Collections.Generic.List<int>();

            for (int i = 0; i < GameManager._MaxSlots; i++)
            {
                if (FactSaveSystem.SlotExist(i))
                {
                    used++;
                    usedIds.Add(i);
                }
            }

            sb.AppendLine($"- Used slots : {used} / {GameManager._MaxSlots}");

            if (used > 0)
                sb.AppendLine($"- Slot IDs   : {string.Join(", ", usedIds)}");
            else
                sb.AppendLine("- Aucune sauvegarde trouvée.");

            sb.AppendLine();
        }

        // -------- LOCALISATION ----------
        private static void AppendLanguageInfo(StringBuilder sb)
        {
            sb.AppendLine("[Localization]");

            string lang = "unknown";
            if (GameManager.Facts.TryGetFact("language", out string factLang))
                lang = factLang;

            sb.AppendLine($"- Current language (Fact) : {lang}");
            sb.AppendLine();
        }
    }
}

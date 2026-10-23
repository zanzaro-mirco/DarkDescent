using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Editor;
using DarkDescent.Progression;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le fasce delle misure del loot (D13 della M13) e delle difficoltà (D10 della M14), comuni ai
    /// test di ogni push e alla ronda notturna (passo 15.5): null se la misura ci sta, altrimenti il
    /// primo errore, con il numero misurato accanto alla fascia.
    /// </summary>
    public static class CensusChecks
    {
        /// <summary>
        /// Quanto una difficoltà può cominciare più facile della precedente (Mirco, 23 ott 2026): alla
        /// profondità 1 Tenebra, Abisso I e II sono quasi pari, e con i semi della notte la misura
        /// oscilla del 3% (passo 15.5). Il 5% non si sente giocando.
        /// </summary>
        public const double StepTolerance = 0.05;

        /// <summary>In media da 4 a 5 oggetti a livello, un po' di margine sopra; rari dal 10 al 25%, magici dal 25 al 35%.</summary>
        public static string Loot(LootCensus.Tally total)
        {
            return Outside("oggetti a livello", total.PerLevel, 4.0, 5.2)
                ?? Outside("parte dei rari", total.Rare / total.Items, 0.1, 0.25)
                ?? Outside("parte dei magici", total.Magic / total.Items, 0.25, 0.35);
        }

        /// <summary>I numeri del loot accanto alle fasce, per il log e il rapporto.</summary>
        public static string DescribeLoot(LootCensus.Tally total)
        {
            return $"{total.Levels} livelli: {total.PerLevel:F2} oggetti a livello (4,0–5,2), rari {total.Rare / total.Items:P1} (10–25%), "
                + $"magici {total.Magic / total.Items:P1} (25–35%)";
        }

        /// <summary>
        /// A ogni difficoltà e profondità: scheletro in 1–5 colpi, bruto in 4–14; il cavaliere regge
        /// almeno 3 colpi del bruto e 2 del re; colpisce il bruto almeno 2 volte su 3, e il bruto lo
        /// colpisce tra il 5% e il 45% delle volte.
        /// </summary>
        public static string EveryDepth(IEnumerable<DifficultyCensus.Row> rows)
        {
            foreach (var row in rows)
            {
                string error = Outside("colpi allo scheletro", row.SkeletonSwings, 1.0, 5.0)
                    ?? Outside("colpi al bruto", row.BruteSwings, 4.0, 14.0)
                    ?? Outside("colpi del bruto retti", row.BruteHitsTaken, 3.0, double.MaxValue)
                    ?? Outside("colpi del re retti", row.KingHitsTaken, 2.0, double.MaxValue)
                    ?? Outside("colpi a segno sul bruto", row.HeroLandsOnBrute, 0.65, 1.0)
                    ?? Outside("colpi del bruto a segno", row.BruteLandsOnHero, 0.05, 0.45);
                if (error != null)
                {
                    return $"{row.TierName} profondità {row.Depth}: {error}";
                }
            }

            return null;
        }

        /// <summary>
        /// Dall'Ombra ogni discesa comincia dura (bruto in 9–14 colpi alla profondità 1), e non più
        /// facile della precedente, a meno di <see cref="StepTolerance"/>.
        /// </summary>
        public static string EachDifficultyStartsHarder(IReadOnlyList<DifficultyCensus.Row> rows, int tiers)
        {
            double previous = 0.0;
            for (int t = 1; t < tiers; t++)
            {
                var first = rows.Single(r => r.Tier == t && r.Depth == 1);
                string error = Outside("colpi al bruto alla profondità 1", first.BruteSwings, 9.0, 14.0);
                if (error != null)
                {
                    return $"{first.TierName}: {error}";
                }

                if (first.BruteSwings < previous * (1.0 - StepTolerance))
                {
                    return $"{first.TierName}: il bruto alla profondità 1 cade in {first.BruteSwings:F2} colpi, oltre il 5% meno dei {previous:F2} della difficoltà prima";
                }

                previous = first.BruteSwings;
            }

            return null;
        }

        /// <summary>
        /// Il re dura un po' di più a ogni gradino: non meno colpi del precedente, a meno di
        /// <see cref="StepTolerance"/>, e meno di una volta e mezza.
        /// </summary>
        public static string KingLastsALittleLonger(IReadOnlyList<DifficultyCensus.Row> rows)
        {
            var kings = rows.Where(r => r.Depth == 8).OrderBy(r => r.Tier).ToList();
            for (int t = 1; t < kings.Count; t++)
            {
                double before = kings[t - 1].KingSwings, now = kings[t].KingSwings;
                if (now < before * (1.0 - StepTolerance) || now >= before * 1.5)
                {
                    return $"{kings[t].TierName}: il re cade in {now:F2} colpi, dopo i {before:F2} della difficoltà prima (almeno il 95%, meno di una volta e mezza)";
                }
            }

            return null;
        }

        /// <summary>
        /// Con una discesa per difficoltà il cavaliere comincia ognuna al più due livelli sotto la zona,
        /// e arriva al massimo prima dell'Abisso IV.
        /// </summary>
        public static string ExperienceKeepsUp(IReadOnlyList<DifficultyCensus.Row> rows, DifficultyTable table, ProgressionSettings settings)
        {
            for (int t = 1; t < table.Count; t++)
            {
                var first = rows.Single(r => r.Tier == t && r.Depth == 1);
                int zone = settings.ZoneLevel(1, table.Tier(t).ZoneLevelBonus);
                int least = Math.Min(settings.MaxLevel, zone - 2);
                if (first.HeroLevel < least)
                {
                    return $"{first.TierName}: il cavaliere comincia al livello {first.HeroLevel}, sotto il {least}";
                }
            }

            int abyss = rows.Single(r => r.Tier == 6 && r.Depth == 1).HeroLevel;
            return abyss == settings.MaxLevel ? null : $"all'Abisso IV il cavaliere è al livello {abyss}, non al {settings.MaxLevel}";
        }

        /// <summary>Le misure delle difficoltà dalla più bassa alla più alta, accanto alle fasce, per il log e il rapporto.</summary>
        public static string DescribeDifficulties(IReadOnlyList<DifficultyCensus.Row> rows)
        {
            string Span(string what, Func<DifficultyCensus.Row, double> value, string band)
            {
                return $"{what} {rows.Min(value):F2}–{rows.Max(value):F2} ({band})";
            }

            return string.Join("; ", Span("colpi allo scheletro", r => r.SkeletonSwings, "1–5"), Span("colpi al bruto", r => r.BruteSwings, "4–14"),
                Span("colpi del bruto retti", r => r.BruteHitsTaken, "≥ 3"), Span("colpi del re retti", r => r.KingHitsTaken, "≥ 2"),
                Span("a segno sul bruto", r => r.HeroLandsOnBrute, "≥ 0,65"), Span("del bruto a segno", r => r.BruteLandsOnHero, "0,05–0,45"));
        }

        private static string Outside(string what, double value, double least, double most)
        {
            if (value >= least && value <= most)
            {
                return null;
            }

            string band = most == double.MaxValue ? $"almeno {least:0.##}" : $"{least:0.##}–{most:0.##}";
            return $"{what} {value:F3}, fuori da {band}";
        }
    }
}

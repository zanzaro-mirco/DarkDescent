using System;
using System.Collections.Generic;
using DarkDescent.Core;

namespace DarkDescent.Enemies
{
    /// <summary>
    /// Quali modificatori dei campioni prende il re a una difficoltà (D5 della M14): tanti quanti ne
    /// dice il gradino, tutti diversi, scelti dal seme del mondo. Logica pura: lo stesso mondo ha
    /// sempre lo stesso re.
    /// </summary>
    public static class BossModifiers
    {
        /// <summary>
        /// Mette in <paramref name="into"/>, svuotata prima, <paramref name="count"/> id presi da
        /// <paramref name="ids"/> senza ripetizioni (tutti, se sono di meno), nell'ordine del tiro.
        /// </summary>
        public static void Pick(IReadOnlyList<string> ids, int count, ulong runSeed, List<string> into)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            into.Clear();
            into.AddRange(ids);
            int take = Math.Max(0, Math.Min(count, into.Count));

            // Fisher-Yates solo per i primi: il resto si butta
            var random = new SplitMix64Source(SeedMixer.ForBoss(runSeed));
            for (int i = 0; i < take; i++)
            {
                int j = i + (int)(random.NextDouble() * (into.Count - i));
                (into[i], into[j]) = (into[j], into[i]);
            }

            into.RemoveRange(take, into.Count - take);
        }
    }
}

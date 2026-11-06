using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Le regole dei segreti delle casse (D3, D4 e D8 della M25): quale cassa normale di un livello è un mimic e quale è il baule
    /// maledetto. Si decide sulle casse che il livello ha già, al caricamento: la mappa, il numero e l'ordine delle casse e dei nemici
    /// non cambiano, e un ricordo di prima (ADR-079) combacia ancora. Ogni scelta è una funzione del seme del mondo, della profondità e
    /// della cella (<see cref="Core.SeedMixer.ForSecret"/>): lo stesso mondo dà gli stessi segreti.
    /// </summary>
    public static class SecretRules
    {
        /// <summary>Dalla profondità 2 una cassa su otto è un mimic (D3).</summary>
        public const int MimicFirstDepth = 2;

        public const int MimicOneIn = 8;

        /// <summary>Dalla profondità 3 un livello su due ha un baule maledetto, uno solo (D4).</summary>
        public const int CursedFirstDepth = 3;

        public const int WaveMin = 3;

        public const int WaveMax = 5;

        private const ulong MimicKind = 1UL;
        private const ulong CursedLevelKind = 2UL;
        private const ulong CursedPickKind = 3UL;
        private const ulong WaveKind = 4UL;

        /// <summary>La cassa in questa cella è un mimic: una su otto, dalla profondità 2.</summary>
        public static bool IsMimic(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return depth >= MimicFirstDepth && Core.SeedMixer.ForSecret(runSeed, depth, cellX, cellZ, MimicKind) % MimicOneIn == 0;
        }

        /// <summary>Questo livello ha un baule maledetto: uno su due, dalla profondità 3.</summary>
        public static bool LevelHasCursed(ulong runSeed, int depth)
        {
            return depth >= CursedFirstDepth && Core.SeedMixer.ForSecret(runSeed, depth, 0, 0, CursedLevelKind) % 2UL == 0UL;
        }

        /// <summary>
        /// L'indice, tra le celle delle casse normali del livello (tutte, anche quelle già aperte: così la scelta non cambia aprendole), del
        /// baule maledetto; -1 se il livello non ne ha. Vince la cella con il punteggio più alto; a parità, la prima. Un mimic non è mai il
        /// baule: lo decide chi chiama, saltando le celle che <see cref="IsMimic"/> segna.
        /// </summary>
        public static int PickCursed(ulong runSeed, int depth, IReadOnlyList<Vector2Int> cells)
        {
            if (!LevelHasCursed(runSeed, depth))
            {
                return -1;
            }

            int best = -1;
            ulong bestScore = 0UL;
            for (int i = 0; i < cells.Count; i++)
            {
                if (IsMimic(runSeed, depth, cells[i].x, cells[i].y))
                {
                    continue;
                }

                ulong score = Core.SeedMixer.ForSecret(runSeed, depth, cells[i].x, cells[i].y, CursedPickKind);
                if (best < 0 || score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>Quanti nemici chiama l'ondata del baule maledetto: da 3 a 5 (D4).</summary>
        public static int WaveSize(ulong runSeed, int depth, int cellX, int cellZ)
        {
            return WaveMin + (int)(Core.SeedMixer.ForSecret(runSeed, depth, cellX, cellZ, WaveKind) % (ulong)(WaveMax - WaveMin + 1));
        }

        /// <summary>Quanti dei nemici dell'ondata sono bruti: nessuno alla cripta tranne il quinto, due alle caverne.</summary>
        public static int WaveBrutes(int depth, int size)
        {
            if (depth >= 5)
            {
                return 2;
            }

            return size >= WaveMax ? 1 : 0;
        }
    }
}

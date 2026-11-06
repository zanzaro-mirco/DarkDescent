using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La statua che si anima (D7 della M25): in un livello su due della cripta, dalla profondità 2 alla 4, una statua di pietra sta vicino a una cassa e si
    /// scrolla quando il cavaliere si avvicina. Quale cassa è una funzione del seme e delle casse del livello (tutte, anche quelle aperte: così la scelta
    /// non cambia aprendole), come per il baule maledetto: il livello generato non cambia e il ricordo (ADR-079) trova il segreto allo stesso posto.
    /// </summary>
    public static class StatueRules
    {
        /// <summary>La cripta, come la stanza sigillata (D16): le caverne non hanno stanze dove mettere una statua.</summary>
        public const int FirstDepth = 2;

        public const int LastDepth = 4;

        /// <summary>
        /// La statua non sta in una cassa a meno di tanti metri dal baule maledetto: la stanza del baule non ha anche una statua (D7). Una stanza ne misura
        /// una dozzina, e il baule chiama già un'ondata.
        /// </summary>
        public const float CursedSeparation = 14f;

        private const ulong LevelKind = 14UL;
        private const ulong PickKind = 15UL;

        /// <summary>Questo livello vuole una statua: uno su due, dalla profondità 2 alla 4.</summary>
        public static bool LevelWantsOne(ulong runSeed, int depth)
        {
            return depth >= FirstDepth && depth <= LastDepth && Core.SeedMixer.ForSecret(runSeed, depth, 0, 0, LevelKind) % 2UL == 0UL;
        }

        /// <summary>
        /// Il tetto di tre segreti per livello (D16): la stanza sigillata, il prigioniero, la statua e il baule maledetto sono i quattro che contano (lore e mimic
        /// no). Se ci sono già gli altri tre, la statua cede: è la prima da tagliare nella scheda e non cambia niente di quello che c'era prima.
        /// </summary>
        public static bool Yields(bool sealedRoom, bool prisoner, bool cursedChest)
        {
            return sealedRoom && prisoner && cursedChest;
        }

        /// <summary>
        /// L'indice, tra le posizioni delle casse che possono avere la statua, di quella che l'ha; -1 se non ce n'è una. Vince il punteggio più alto del seme
        /// sulla posizione (decimetri, come <c>Chest.Cell</c>).
        /// </summary>
        public static int Pick(ulong runSeed, int depth, IReadOnlyList<Vector2Int> cells)
        {
            int best = -1;
            ulong bestScore = 0UL;
            for (int i = 0; i < cells.Count; i++)
            {
                ulong score = Core.SeedMixer.ForSecret(runSeed, depth, cells[i].x, cells[i].y, PickKind);
                if (best < 0 || score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }

            return best;
        }
    }
}

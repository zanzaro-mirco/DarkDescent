using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il prigioniero (D6 della M25): un cavaliere in catene, seduto contro un muro di una stanza dove ci sono già dei nemici: sono la sua scorta, e il livello non
    /// cambia (stesso numero di nemici nello stesso posto, il ricordo di ADR-079 combacia). Si libera con un click quando la scorta è morta: dà una pozione, un
    /// po' d'oro e un consiglio, poi sale verso la scala. Un livello su tre della cripta, dalla profondità 2 alla 4. Funzioni pure della mappa e del seme.
    /// </summary>
    public static class PrisonerRules
    {
        public const int FirstDepth = 2;

        public const int LastDepth = 4;

        /// <summary>Quanti consigli ha (<c>prisoner.advice.1</c> … <c>.4</c>).</summary>
        public const int AdviceCount = 4;

        /// <summary>Le celle attorno al prigioniero (distanza massima in celle, in ogni direzione) dove si contano i nemici della scorta.</summary>
        public const int EscortCells = 3;

        private const ulong LevelKind = 12UL;
        private const ulong CellKind = 13UL;
        private const ulong AdviceKind = 16UL;

        /// <summary>Questo livello vuole un prigioniero: uno su tre, dalla profondità 2 alla 4.</summary>
        public static bool LevelWantsOne(ulong runSeed, int depth)
        {
            return depth >= FirstDepth && depth <= LastDepth && Core.SeedMixer.ForSecret(runSeed, depth, 0, 0, LevelKind) % 3UL == 0UL;
        }

        /// <summary>
        /// Il posto del prigioniero, o null se il livello non ha una stanza con almeno un nemico accanto a un muro libero (al meglio due). Come per gli altri
        /// segreti, funzione della mappa e del seme: sceglie il punteggio più alto tra le celle contro un muro con la scorta attorno.
        /// </summary>
        public static SecretSpots.Spot? Place(LevelMap map, ulong runSeed, int depth, IReadOnlyCollection<Vector2Int> avoid, ISet<Vector2Int> exclude)
        {
            var walkable = SealedRoomPlanner.Walkability(map);
            for (int wanted = 2; wanted >= 1; wanted--)
            {
                int minimum = wanted;
                var spot = SecretSpots.Pick(map, walkable, runSeed, depth, CellKind, avoid, exclude, 6, cell => Escort(map, cell, exclude) >= minimum);
                if (spot != null)
                {
                    return spot;
                }
            }

            return null;
        }

        /// <summary>Quanti nemici ci sono nelle celle attorno a quella (fuori dal fondo sigillato, che ha i suoi): la scorta che il prigioniero avrebbe.</summary>
        public static int Escort(LevelMap map, Vector2Int cell, ISet<Vector2Int> exclude = null)
        {
            int count = 0;
            for (int dx = -EscortCells; dx <= EscortCells; dx++)
            {
                for (int dy = -EscortCells; dy <= EscortCells; dy++)
                {
                    var other = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (other.x < 0 || other.y < 0 || other.x >= map.Width || other.y >= map.Height || (exclude != null && exclude.Contains(other)))
                    {
                        continue;
                    }

                    char symbol = map.GetSymbol(other.x, other.y);
                    if (symbol == DungeonPopulator.EnemySymbol || symbol == SpecialEnemyPlanner.UniqueSymbol || symbol == DungeonPopulator.MageSymbol)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Il consiglio del prigioniero (1–4): dal seme e dalla profondità, nessun tiro.</summary>
        public static int AdviceFor(ulong runSeed, int depth)
        {
            return 1 + (int)(Core.SeedMixer.ForSecret(runSeed, depth, 0, 0, AdviceKind) % (ulong)AdviceCount);
        }

        /// <summary>L'oro del ringraziamento: poco, una cassa ne dà di più alle stesse profondità (D8: i segreti non sono una strada più facile).</summary>
        public static int Gold(int depth)
        {
            return 15 * (depth + 1);
        }

        public static string AdviceKey(int advice)
        {
            return $"prisoner.advice.{advice}";
        }
    }
}

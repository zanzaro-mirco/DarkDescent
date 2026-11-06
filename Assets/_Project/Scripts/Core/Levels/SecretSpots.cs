using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il posto di un arredo dei segreti (la lapide e il leggio della lore, il prigioniero, la statua; M25): una cella di pavimento semplice contro un muro,
    /// dentro una stanza, lontano da ingresso, scale, porta, leva e dagli altri segreti, e senza ostacoli attorno. Come per la leva, il posto è una funzione
    /// della mappa e del seme: lo stesso livello dà lo stesso posto a ogni caricamento, e il ricordo (ADR-079) lo ritrova. Non cambia niente nella mappa.
    /// </summary>
    public static class SecretSpots
    {
        /// <summary>Un posto: la cella e da che lato guarda (verso lo spazio libero, con il muro alle spalle).</summary>
        public readonly struct Spot
        {
            public Spot(Vector2Int cell, Vector2Int facing)
            {
                Cell = cell;
                Facing = facing;
            }

            public Vector2Int Cell { get; }

            public Vector2Int Facing { get; }
        }

        /// <summary>
        /// Il posto col punteggio più alto del dominio <paramref name="kind"/> del seme, o null se il livello non ne ha uno. <paramref name="avoid"/>: le celle dei
        /// segreti già messi, da tenere a <paramref name="gap"/> passi di distanza. <paramref name="exclude"/>: celle dove non va (il fondo della stanza sigillata).
        /// </summary>
        public static Spot? Pick(LevelMap map, ulong runSeed, int depth, ulong kind, IReadOnlyCollection<Vector2Int> avoid, ISet<Vector2Int> exclude, int gap = 6)
        {
            return Pick(map, SealedRoomPlanner.Walkability(map), runSeed, depth, kind, avoid, exclude, gap);
        }

        /// <summary>Come sopra, con la mappa del cammino già calcolata (chi sceglie più posti la calcola una volta).</summary>
        public static Spot? Pick(LevelMap map, bool[,] walkable, ulong runSeed, int depth, ulong kind, IReadOnlyCollection<Vector2Int> avoid, ISet<Vector2Int> exclude, int gap = 6)
        {
            SealedRoomPlanner.TryFindSpecial(map, DungeonGenerator.EntranceSymbol, out var entrance);
            SealedRoomPlanner.TryFindSpecial(map, DungeonGenerator.StairsSymbol, out var stairs);
            Spot? best = null;
            ulong bestScore = 0UL;
            for (int x = 2; x < map.Width - 2; x++)
            {
                for (int y = 2; y < map.Height - 2; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!IsFree(map, walkable, cell) || Manhattan(cell, entrance) < 4 || Manhattan(cell, stairs) < 4 || (exclude != null && exclude.Contains(cell)))
                    {
                        continue;
                    }

                    if (!SealedRoomPlanner.AgainstAWall(map, walkable, cell, out var facing))
                    {
                        continue;
                    }

                    // davanti, libero e camminabile: si usa da lì
                    var front = cell + facing;
                    if (!walkable[front.x, front.y] || map.GetSymbol(front.x, front.y) != LevelMap.Floor || (exclude != null && exclude.Contains(front)))
                    {
                        continue;
                    }

                    if (TooClose(cell, avoid, gap))
                    {
                        continue;
                    }

                    ulong score = Core.SeedMixer.ForSecret(runSeed, depth, x, y, kind);
                    if (best == null || score > bestScore)
                    {
                        best = new Spot(cell, facing);
                        bestScore = score;
                    }
                }
            }

            return best;
        }

        // pavimento semplice e nessun ostacolo (casse, arredi) nelle otto celle attorno: l'arredo non chiude un passo e non nasconde una cassa
        private static bool IsFree(LevelMap map, bool[,] walkable, Vector2Int cell)
        {
            if (map.GetSymbol(cell.x, cell.y) != LevelMap.Floor || !walkable[cell.x, cell.y])
            {
                return false;
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (DungeonPopulator.IsObstacle(map.GetSymbol(cell.x + dx, cell.y + dy)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TooClose(Vector2Int cell, IReadOnlyCollection<Vector2Int> avoid, int gap)
        {
            if (avoid == null)
            {
                return false;
            }

            foreach (var other in avoid)
            {
                if (Manhattan(cell, other) < gap)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}

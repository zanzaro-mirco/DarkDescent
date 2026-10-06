using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il contenuto di una caverna (passo 7.2 della M7). Le caverne non hanno stanze: si ragiona sulle
    /// celle e sui passi dall'ingresso. Ci sono candele a terra contro la roccia, perché le torce non
    /// ci sono (D4); casse e scenografia contro la roccia; scheletri a gruppi lontano dall'ingresso.
    /// I simboli sono quelli della cripta, così mappa, builder e automappa non cambiano: il tileset
    /// delle caverne dà a barile, casse e pilastro i suoi modelli. Una classe a parte dal popolatore
    /// della cripta, che così non cambia i livelli dei semi già provati (trappola 7). Logica pura.
    /// </summary>
    public sealed class CavePopulator
    {
        public const char CandleSymbol = 'l';

        /// <summary>Un nemico dello sciame (D5): arriva a gruppi.</summary>
        public const char SwarmSymbol = 'w';

        /// <summary>Un bruto (D6): da solo, grosso e lento.</summary>
        public const char BruteSymbol = 'B';

        /// <summary>Un simbolo di nemico delle caverne: scheletro, sciame o bruto.</summary>
        public static bool IsEnemy(char symbol)
        {
            return symbol == DungeonPopulator.EnemySymbol || symbol == SwarmSymbol || symbol == BruteSymbol;
        }

        private const string PropSymbols = "bxp";

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly CaveSettings _settings;

        public CavePopulator(CaveSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public void Populate(DungeonLayout layout, int depth, IRandomSource random)
        {
            // come nella cripta: davanti e ai lati della scala, e attorno al cavaliere, niente
            var reserved = new HashSet<Vector2Int>
            {
                layout.Stairs + Vector2Int.up, layout.Stairs + Vector2Int.left, layout.Stairs + Vector2Int.right,
            };
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    reserved.Add(layout.Entrance + new Vector2Int(dx, dy));
                }
            }

            int[,] steps = Distances(layout);
            var floor = new List<Vector2Int>();
            var againstRock = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (layout[x, y] != LevelMap.Floor)
                    {
                        continue;
                    }

                    var cell = new Vector2Int(x, y);
                    floor.Add(cell);
                    if (RockBeside(layout, cell) > 0)
                    {
                        againstRock.Add(cell);
                    }
                }
            }

            PlaceCandles(layout, Shuffled(againstRock, random), floor.Count, reserved);

            // casse e scenografia dove non si passa: contro la roccia, e se chiudono il cammino si tolgono
            var far = againstRock.FindAll(c => steps[c.x, c.y] >= _settings.QuietSteps);
            PlaceObstacles(layout, Shuffled(far, random), _settings.ChestCount(depth), () => DungeonPopulator.ChestSymbol, reserved);
            PlaceObstacles(layout, Shuffled(againstRock, random), floor.Count / _settings.FloorPerProp,
                () => PropSymbols[Range(random, 0, PropSymbols.Length - 1)], reserved);

            PlaceEnemies(layout, depth, random, steps, reserved);
        }

        // Una ogni FloorPerCandle celle, ognuna ad almeno CandleSpacing celle dalle altre: la luce si sparge.
        private void PlaceCandles(DungeonLayout layout, List<Vector2Int> candidates, int floorCount, HashSet<Vector2Int> reserved)
        {
            int wanted = floorCount / _settings.FloorPerCandle;
            var placed = new List<Vector2Int>();
            foreach (var cell in candidates)
            {
                if (placed.Count >= wanted)
                {
                    break;
                }

                if (reserved.Contains(cell) || placed.Exists(p => Chebyshev(p, cell) < _settings.CandleSpacing))
                {
                    continue;
                }

                if (layout.TryPlace(CandleSymbol, cell.x, cell.y))
                {
                    placed.Add(cell);
                }
            }
        }

        private static void PlaceObstacles(DungeonLayout layout, List<Vector2Int> candidates, int wanted, Func<char> symbol, HashSet<Vector2Int> reserved)
        {
            int placed = 0;
            foreach (var cell in candidates)
            {
                if (placed >= wanted)
                {
                    break;
                }

                // una cella tra due rocce è un passaggio: un ostacolo lo chiuderebbe del tutto
                if (reserved.Contains(cell) || IsPassage(layout, cell) || !layout.TryPlace(symbol(), cell.x, cell.y))
                {
                    continue;
                }

                if (IsConnected(layout))
                {
                    placed++;
                }
                else
                {
                    layout.Remove(cell.x, cell.y);
                }
            }
        }

        // A gruppi, ognuno attorno a un centro lontano dall'ingresso e dagli altri gruppi.
        private void PlaceEnemies(DungeonLayout layout, int depth, IRandomSource random, int[,] steps, HashSet<Vector2Int> reserved)
        {
            var centers = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (layout[x, y] == LevelMap.Floor && steps[x, y] >= _settings.QuietSteps)
                    {
                        centers.Add(new Vector2Int(x, y));
                    }
                }
            }

            centers = Shuffled(centers, random);
            var used = new List<Vector2Int>();
            int next = 0;

            // quanti di ogni tipo dice la tabella della profondità (D10)
            var row = _settings.SpawnTable.For(depth);
            int swarms = Range(random, row.SwarmGroups.x, row.SwarmGroups.y);
            int brutes = Range(random, row.Brutes.x, row.Brutes.y);
            int skeletons = Range(random, row.SkeletonGroups.x, row.SkeletonGroups.y);

            // prima lo sciame, che arriva a gruppi interi e vuole spazio; poi i bruti, da soli e
            // lontani tra loro e dai gruppi; per ultimi gli scheletri
            for (int g = 0; g < swarms; g++)
            {
                int size = Range(random, _settings.MinSwarm, _settings.MaxSwarm);
                PlaceGroup(layout, random, steps, reserved, centers, used, ref next, CavePopulator.SwarmSymbol, size);
            }

            for (int b = 0; b < brutes; b++)
            {
                PlaceGroup(layout, random, steps, reserved, centers, used, ref next, CavePopulator.BruteSymbol, 1);
            }

            for (int g = 0; g < skeletons; g++)
            {
                int size = Range(random, _settings.MinGroup, _settings.MaxGroup);
                PlaceGroup(layout, random, steps, reserved, centers, used, ref next, DungeonPopulator.EnemySymbol, size);
            }
        }

        // Un gruppo intero attorno al prossimo centro libero, ad almeno 4 celle dagli altri gruppi: nelle
        // 3 × 3 celle attorno, quelle libere e lontane dall'ingresso. Restituisce quanti ne ha messi.
        private int PlaceGroup(DungeonLayout layout, IRandomSource random, int[,] steps, HashSet<Vector2Int> reserved,
            List<Vector2Int> centers, List<Vector2Int> used, ref int next, char symbol, int size)
        {
            while (next < centers.Count)
            {
                var center = centers[next++];
                if (used.Exists(c => Chebyshev(c, center) < 4))
                {
                    continue;
                }

                var near = new List<Vector2Int>();
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var cell = center + new Vector2Int(dx, dy);
                        if (layout[cell.x, cell.y] == LevelMap.Floor && !reserved.Contains(cell) && steps[cell.x, cell.y] >= _settings.QuietSteps)
                        {
                            near.Add(cell);
                        }
                    }
                }

                // in un cunicolo il gruppo non ci sta: si cerca un centro più aperto
                if (near.Count < size)
                {
                    continue;
                }

                used.Add(center);
                int placed = 0;
                for (; placed < size && near.Count > 0; placed++)
                {
                    int pick = Range(random, 0, near.Count - 1);
                    layout.TryPlace(symbol, near[pick].x, near[pick].y);
                    near.RemoveAt(pick);
                }

                return placed;
            }

            return 0;
        }

        // Passi a piedi dall'ingresso sul pavimento della caverna appena generata; -1 dove non si arriva.
        private static int[,] Distances(DungeonLayout layout)
        {
            var cells = new char[layout.Width, layout.Height];
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    cells[x, y] = layout[x, y];
                }
            }

            return LevelGrid.Distances(cells, layout.Entrance);
        }

        private static int RockBeside(DungeonLayout layout, Vector2Int cell)
        {
            int count = 0;
            foreach (var step in Steps)
            {
                if (!layout.IsFloor(cell.x + step.x, cell.y + step.y))
                {
                    count++;
                }
            }

            return count;
        }

        // Roccia da due parti opposte: la cella è un cunicolo largo una, e ci si deve passare.
        private static bool IsPassage(DungeonLayout layout, Vector2Int cell)
        {
            bool northSouth = !layout.IsFloor(cell.x, cell.y - 1) && !layout.IsFloor(cell.x, cell.y + 1);
            bool eastWest = !layout.IsFloor(cell.x - 1, cell.y) && !layout.IsFloor(cell.x + 1, cell.y);
            return northSouth || eastWest;
        }

        // Dall'ingresso si arriva a piedi su ogni cella che non sia roccia, scala o ostacolo.
        private static bool IsConnected(DungeonLayout layout)
        {
            var seen = new bool[layout.Width, layout.Height];
            var queue = new Queue<Vector2Int>();
            seen[layout.Entrance.x, layout.Entrance.y] = true;
            queue.Enqueue(layout.Entrance);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (Walkable(layout, next.x, next.y) && !seen[next.x, next.y])
                    {
                        seen[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }
            }

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (Walkable(layout, x, y) && !seen[x, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Walkable(DungeonLayout layout, int x, int y)
        {
            char c = layout[x, y];
            return c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(c);
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        // Una copia mescolata dal seme: l'ordine della lista originale non conta.
        private static List<Vector2Int> Shuffled(List<Vector2Int> cells, IRandomSource random)
        {
            var copy = new List<Vector2Int>(cells);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = Range(random, 0, i);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }

            return copy;
        }

        private static int Range(IRandomSource random, int min, int max)
        {
            return Math.Min(max, min + (int)(random.NextDouble() * (max - min + 1)));
        }
    }
}

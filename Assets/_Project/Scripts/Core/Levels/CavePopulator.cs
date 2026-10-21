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

        /// <summary>Il boss (D8 della M10): al centro della sua sala.</summary>
        public const char BossSymbol = 'K';

        /// <summary>La porta della sala del boss, che si chiude quando comincia lo scontro.</summary>
        public const char GateSymbol = 'G';

        /// <summary>Un simbolo di nemico: scheletro, sciame, bruto, mago, boss o l'unico della M12.</summary>
        public static bool IsEnemy(char symbol)
        {
            return symbol == DungeonPopulator.EnemySymbol || symbol == SwarmSymbol || symbol == BruteSymbol || symbol == DungeonPopulator.MageSymbol
                || symbol == BossSymbol || symbol == SpecialEnemyPlanner.UniqueSymbol;
        }

        /// <summary>
        /// I temi degli arredi delle caverne (D4 della M11): cristalli, ossa, frane. Tre composizioni
        /// per tema, una zona della caverna per tema.
        /// </summary>
        public static readonly string[] DecorThemes = { "ACH", "IJM", "PQX" };

        private const string DecorSymbols = "ACHIJMPQX";

        /// <summary>Le zone dei temi: quadrati di tante celle di lato, dall'angolo della mappa.</summary>
        public const int DecorZone = 7;

        /// <summary>Una composizione degli arredi delle caverne.</summary>
        public static bool IsDecor(char symbol)
        {
            return DecorSymbols.IndexOf(symbol) >= 0;
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
            var reserved = Reserved(layout);
            if (layout.HasBossRoom)
            {
                PlaceBossRoom(layout);
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

        // La sala del boss resta sua (D8 della M10): il boss al centro, la porta, una candela per angolo;
        // niente altro dentro né sulla soglia, dove la porta deve potersi chiudere.
        private static void PlaceBossRoom(DungeonLayout layout)
        {
            var center = layout.BossCell;
            layout.TryPlace(BossSymbol, center.x, center.y);
            int half = CaveGenerator.BossRoomHalf;
            foreach (var corner in new[] { new Vector2Int(-half, -half), new Vector2Int(half, -half), new Vector2Int(-half, half), new Vector2Int(half, half) })
            {
                layout.TryPlace(CandleSymbol, center.x + corner.x, center.y + corner.y);
            }

            var gate = layout.GateCell;
            if (gate.x >= 0)
            {
                layout.TryPlace(GateSymbol, gate.x, gate.y);
            }
        }

        // Come nella cripta: davanti e ai lati della scala, e attorno al cavaliere, niente. La sala del
        // boss resta sua, con un giro di celle attorno, e oltre la porta si deve poter passare.
        private static HashSet<Vector2Int> Reserved(DungeonLayout layout)
        {
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

            if (layout.HasBossRoom)
            {
                const int Ring = CaveGenerator.BossRoomHalf + 1;
                for (int dy = -Ring - 1; dy <= Ring + 1; dy++)
                {
                    for (int dx = -Ring - 1; dx <= Ring + 1; dx++)
                    {
                        reserved.Add(layout.BossCell + new Vector2Int(dx, dy));
                    }
                }

                var gate = layout.GateCell;
                if (gate.x >= 0)
                {
                    foreach (var step in Steps)
                    {
                        reserved.Add(gate + step + step);
                    }
                }
            }

            return reserved;
        }

        /// <summary>
        /// Gli arredi (D4 della M11), dopo tutto il resto e con un flusso suo del seme: mappa, nemici e
        /// casse dei semi già provati non cambiano. La caverna è divisa in zone quadrate con un tema
        /// ciascuna; le composizioni stanno contro la roccia, lontane tra loro, mai in un cunicolo, mai
        /// accanto a una cassa o alla scala, e se chiudono il cammino si tolgono.
        /// </summary>
        public void Decorate(DungeonLayout layout, IRandomSource random)
        {
            if (_settings.FloorPerDecor <= 0)
            {
                return;
            }

            int zonesX = (layout.Width + DecorZone - 1) / DecorZone;
            int zonesY = (layout.Height + DecorZone - 1) / DecorZone;
            var themes = new string[zonesX, zonesY];
            for (int y = 0; y < zonesY; y++)
            {
                for (int x = 0; x < zonesX; x++)
                {
                    themes[x, y] = DecorThemes[Range(random, 0, DecorThemes.Length - 1)];
                }
            }

            var reserved = Reserved(layout);
            var candidates = new List<Vector2Int>();
            int floorCount = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (layout.IsFloor(x, y))
                    {
                        floorCount++;
                    }

                    if (layout[x, y] == LevelMap.Floor && RockBeside(layout, cell) > 0 && !reserved.Contains(cell)
                        && !IsPassage(layout, cell) && !NextToKeep(layout, cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            int wanted = floorCount / _settings.FloorPerDecor;
            var placed = new List<Vector2Int>();
            foreach (var cell in Shuffled(candidates, random))
            {
                if (placed.Count >= wanted)
                {
                    break;
                }

                if (placed.Exists(p => Chebyshev(p, cell) < _settings.DecorSpacing))
                {
                    continue;
                }

                string theme = themes[cell.x / DecorZone, cell.y / DecorZone];
                if (!layout.TryPlace(theme[Range(random, 0, theme.Length - 1)], cell.x, cell.y))
                {
                    continue;
                }

                if (IsConnected(layout))
                {
                    placed.Add(cell);
                }
                else
                {
                    layout.Remove(cell.x, cell.y);
                }
            }

            PlaceWallBarrels(layout, random, candidates, placed);
        }

        // I gruppi di barili contro la roccia (D5 della M13), dopo le composizioni e lontani da loro come
        // loro tra loro: finché la caverna non ha i barili del tiro. Il 'b' delle caverne è roccia, non conta
        private void PlaceWallBarrels(DungeonLayout layout, IRandomSource random, List<Vector2Int> candidates, List<Vector2Int> placed)
        {
            var range = _settings.BarrelsPerLevel;
            int wanted = Range(random, range.x, range.y);
            int count = 0;
            foreach (var cell in Shuffled(candidates, random))
            {
                if (count >= wanted)
                {
                    break;
                }

                if (placed.Exists(p => Chebyshev(p, cell) < _settings.DecorSpacing))
                {
                    continue;
                }

                string symbols = DungeonPopulator.BarrelWallSymbols;
                char symbol = wanted - count <= 2 ? symbols[0] : symbols[Range(random, 0, symbols.Length - 1)];
                if (!layout.TryPlace(symbol, cell.x, cell.y))
                {
                    continue;
                }

                if (IsConnected(layout))
                {
                    placed.Add(cell);
                    count += DungeonPopulator.BarrelsInGroup(symbol);
                }
                else
                {
                    layout.Remove(cell.x, cell.y);
                }
            }
        }

        // accanto a una cassa, alla scala o alla porta del boss: lì il cavaliere deve poter arrivare
        private static bool NextToKeep(DungeonLayout layout, Vector2Int cell)
        {
            foreach (var step in Steps)
            {
                char c = layout[cell.x + step.x, cell.y + step.y];
                if (c == DungeonPopulator.ChestSymbol || c == DungeonGenerator.StairsSymbol || c == GateSymbol)
                {
                    return true;
                }
            }

            return false;
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

            // i maghi per ultimi, da soli: i tiri in più non spostano gli altri (trappola 10 della M7)
            if (row.Mages.y > 0)
            {
                int mages = Range(random, row.Mages.x, row.Mages.y);
                for (int m = 0; m < mages; m++)
                {
                    PlaceGroup(layout, random, steps, reserved, centers, used, ref next, DungeonPopulator.MageSymbol, 1);
                }
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

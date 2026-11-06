using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La stanza sigillata (D5 della M25): un fondo cieco del livello che ha già, chiuso da una porta di pietra su un corridoio, con una leva
    /// in un'altra parte del livello. Non nasce niente nella mappa: le casse e i nemici che stanno già in quel fondo sono il tesoro e la
    /// scorta, quindi il numero e l'ordine di casse e nemici restano quelli del livello generato e un ricordo di prima (ADR-079) combacia
    /// ancora. Una funzione pura della mappa e del seme del mondo, provata su molti semi: la porta sta su un corridoio dritto che separa
    /// dall'ingresso un pezzo di livello senza scale né ingresso, e la leva sta fuori dal pezzo, contro un muro di una stanza, lontano dalla porta.
    /// </summary>
    public static class SealedRoomPlanner
    {
        /// <summary>Dalla profondità 2 alla 4: la cripta (D16 della M25); le caverne non hanno corridoi dove mettere una porta.</summary>
        public const int FirstDepth = 2;

        public const int LastDepth = 4;

        /// <summary>
        /// Un livello su due, tra quelli che hanno un fondo cieco adatto con una cassa (circa due su tre, misurato in 25.4 su 200 semi per profondità): in tutto
        /// circa un livello su tre della cripta dalla profondità 2, come dice D5.
        /// </summary>
        public const int OneLevelIn = 2;

        /// <summary>Quante celle di pavimento almeno ha il fondo chiuso dalla porta: una stanza, non un pezzo di corridoio.</summary>
        public const int MinRegionCells = 8;

        /// <summary>Quante celle di cammino separano al minimo la leva dalla porta (D5: la leva è lontana).</summary>
        public const int MinLeverDistance = 12;

        private const ulong LevelKind = 5UL;
        private const ulong DoorKind = 6UL;
        private const ulong LeverKind = 7UL;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        /// <summary>Il piano di una stanza sigillata: dove sta la porta, quale corridoio chiude, dove la leva e quanto vale il fondo.</summary>
        public sealed class Plan
        {
            public Vector2Int Door { get; set; }

            /// <summary>Il corridoio corre lungo X (vero) o lungo Y (falso): la porta lo chiude di traverso.</summary>
            public bool CorridorAlongX { get; set; }

            public Vector2Int Lever { get; set; }

            /// <summary>Da che lato guarda la leva: verso lo spazio libero, con il muro alle spalle.</summary>
            public Vector2Int LeverFacing { get; set; }

            /// <summary>Le celle di pavimento del fondo chiuso dalla porta.</summary>
            public List<Vector2Int> Region { get; } = new List<Vector2Int>();

            /// <summary>Le casse nel fondo: il tesoro.</summary>
            public int Chests { get; set; }

            /// <summary>I nemici nel fondo: la scorta.</summary>
            public int Enemies { get; set; }

            /// <summary>Quante celle di cammino separano la leva dalla porta.</summary>
            public int LeverDistance { get; set; }
        }

        /// <summary>Questo livello ha la stanza sigillata, se ne ha una adatta: un livello su tre dalla profondità 2 alla 4.</summary>
        public static bool LevelWantsOne(ulong runSeed, int depth)
        {
            return depth >= FirstDepth && depth <= LastDepth && Core.SeedMixer.ForSecret(runSeed, depth, 0, 0, LevelKind) % OneLevelIn == 0UL;
        }

        /// <summary>
        /// Il piano della stanza sigillata del livello, o null se il livello non la vuole (<see cref="LevelWantsOne"/>) o non ha un fondo cieco
        /// adatto con una cassa.
        /// </summary>
        public static Plan For(LevelMap map, ulong runSeed, int depth)
        {
            return LevelWantsOne(runSeed, depth) ? Find(map, runSeed, depth, requireChest: true) : null;
        }

        /// <summary>
        /// Il piano migliore del livello senza guardare se il livello lo vuole: per misurare quanti livelli ne hanno uno e per i test.
        /// <paramref name="requireChest"/>: il fondo deve avere almeno una cassa, il tesoro.
        /// </summary>
        public static Plan Find(LevelMap map, ulong runSeed, int depth, bool requireChest)
        {
            if (!TryFindSpecial(map, DungeonGenerator.EntranceSymbol, out var entrance))
            {
                return null;
            }

            TryFindSpecial(map, DungeonGenerator.StairsSymbol, out var stairs);
            var walkable = Walkability(map);

            Plan best = null;
            ulong bestScore = 0UL;
            int walkableCount = 0;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (walkable[x, y])
                    {
                        walkableCount++;
                    }
                }
            }

            for (int x = 1; x < map.Width - 1; x++)
            {
                for (int y = 1; y < map.Height - 1; y++)
                {
                    var door = new Vector2Int(x, y);
                    if (!IsDoorCell(map, walkable, door, entrance, stairs, out bool alongX))
                    {
                        continue;
                    }

                    var reached = Flood(walkable, map.Width, map.Height, entrance, door);
                    var plan = Region(map, walkable, reached, door, entrance, stairs, walkableCount);
                    if (plan == null || (requireChest && plan.Chests == 0))
                    {
                        continue;
                    }

                    plan.Door = door;
                    plan.CorridorAlongX = alongX;
                    ulong score = Core.SeedMixer.ForSecret(runSeed, depth, x, y, DoorKind);
                    if (best == null || score > bestScore)
                    {
                        best = plan;
                        bestScore = score;
                    }
                }
            }

            if (best == null || !PlaceLever(map, walkable, best, runSeed, depth, entrance, stairs))
            {
                return null;
            }

            return best;
        }

        /// <summary>
        /// Dove si cammina: pavimento senza ostacoli, e non la scala giù. Lo stesso criterio con cui il popolatore controlla che il livello resti
        /// connesso (casse, arredi e oggetti di scena non si attraversano; una cassa si apre dal davanti).
        /// </summary>
        public static bool[,] Walkability(LevelMap map)
        {
            var walkable = new bool[map.Width, map.Height];
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    char symbol = map.GetSymbol(x, y);
                    walkable[x, y] = map.IsFloor(x, y) && symbol != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(symbol);
                }
            }

            return walkable;
        }

        public static bool TryFindSpecial(LevelMap map, char symbol, out Vector2Int cell)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map.GetSymbol(x, y) == symbol)
                    {
                        cell = new Vector2Int(x, y);
                        return true;
                    }
                }
            }

            cell = default;
            return false;
        }

        // una cella di corridoio dritto: pavimento semplice con due lati opposti camminabili e gli altri due roccia, lontano da ingresso e scala
        private static bool IsDoorCell(LevelMap map, bool[,] walkable, Vector2Int cell, Vector2Int entrance, Vector2Int stairs, out bool alongX)
        {
            alongX = false;
            if (map.GetSymbol(cell.x, cell.y) != LevelMap.Floor || !walkable[cell.x, cell.y])
            {
                return false;
            }

            if (Manhattan(cell, entrance) < 4 || Manhattan(cell, stairs) < 4)
            {
                return false;
            }

            bool east = walkable[cell.x + 1, cell.y];
            bool west = walkable[cell.x - 1, cell.y];
            bool north = walkable[cell.x, cell.y + 1];
            bool south = walkable[cell.x, cell.y - 1];
            if (east && west && !north && !south && map.IsRock(cell.x, cell.y + 1) && map.IsRock(cell.x, cell.y - 1))
            {
                alongX = true;
                return true;
            }

            return north && south && !east && !west && map.IsRock(cell.x + 1, cell.y) && map.IsRock(cell.x - 1, cell.y);
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        // le celle che si raggiungono dall'ingresso senza passare da blocked
        public static bool[,] Flood(bool[,] walkable, int width, int height, Vector2Int from, Vector2Int blocked)
        {
            var seen = new bool[width, height];
            var queue = new Queue<Vector2Int>();
            seen[from.x, from.y] = true;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || next == blocked || seen[next.x, next.y] || !walkable[next.x, next.y])
                    {
                        continue;
                    }

                    seen[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return seen;
        }

        // il fondo chiuso dalla porta: il pavimento che l'ingresso non raggiunge più; vale solo se è una stanza, senza ingresso, scala né segni del Custode
        private static Plan Region(LevelMap map, bool[,] walkable, bool[,] reached, Vector2Int door, Vector2Int entrance, Vector2Int stairs, int walkableCount)
        {
            var plan = new Plan();
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (!walkable[x, y] || reached[x, y] || (x == door.x && y == door.y))
                    {
                        continue;
                    }

                    char symbol = map.GetSymbol(x, y);
                    if (symbol == DungeonGenerator.EntranceSymbol || symbol == DungeonPopulator.GuardianSymbol || symbol == DungeonPopulator.GuardianTreasureSymbol)
                    {
                        return null;
                    }

                    plan.Region.Add(new Vector2Int(x, y));
                    if (symbol == DungeonPopulator.EnemySymbol || symbol == SpecialEnemyPlanner.UniqueSymbol || symbol == DungeonPopulator.MageSymbol)
                    {
                        plan.Enemies++;
                    }
                }
            }

            if (plan.Region.Count < MinRegionCells || plan.Region.Count * 5 > walkableCount * 3)
            {
                return null;
            }

            // le casse del fondo (un ostacolo, non camminabile): quelle con un lato dentro il fondo e nessuno fuori; la scala giù e il suo davanti non ci sono
            var chests = new HashSet<Vector2Int>();
            foreach (var cell in plan.Region)
            {
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    char symbol = map.GetSymbol(next.x, next.y);
                    if (symbol == DungeonGenerator.StairsSymbol)
                    {
                        return null;
                    }

                    if (DungeonPopulator.IsChest(symbol))
                    {
                        chests.Add(next);
                    }
                }
            }

            plan.Chests = chests.Count;
            return plan;
        }

        // la leva: una cella di pavimento semplice del livello raggiungibile (con la porta chiusa), contro un muro di una stanza e non alla bocca di un corridoio, lontano dalla porta
        private static bool PlaceLever(LevelMap map, bool[,] walkable, Plan plan, ulong runSeed, int depth, Vector2Int entrance, Vector2Int stairs)
        {
            var distances = Distances(walkable, map.Width, map.Height, plan.Door, plan.Region);
            ulong bestScore = 0UL;
            bool found = false;
            for (int pass = 0; pass < 2 && !found; pass++)
            {
                int minimum = pass == 0 ? MinLeverDistance : MinLeverDistance / 2;
                for (int x = 2; x < map.Width - 2; x++)
                {
                    for (int y = 2; y < map.Height - 2; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (distances[x, y] < minimum || map.GetSymbol(x, y) != LevelMap.Floor || !walkable[x, y] || Manhattan(cell, entrance) < 4 || Manhattan(cell, stairs) < 4)
                        {
                            continue;
                        }

                        if (!AgainstAWall(map, walkable, cell, out var facing))
                        {
                            continue;
                        }

                        // davanti alla leva, libero e camminabile: si tira da lì
                        var front = cell + facing;
                        if (!walkable[front.x, front.y] || map.GetSymbol(front.x, front.y) != LevelMap.Floor)
                        {
                            continue;
                        }

                        ulong score = Core.SeedMixer.ForSecret(runSeed, depth, x, y, LeverKind);
                        if (!found || score > bestScore)
                        {
                            found = true;
                            bestScore = score;
                            plan.Lever = cell;
                            plan.LeverFacing = facing;
                            plan.LeverDistance = distances[x, y];
                        }
                    }
                }
            }

            return found;
        }

        // contro un muro e dentro una stanza: la cella ha roccia da un lato solo e almeno sei delle otto celle attorno camminabili
        public static bool AgainstAWall(LevelMap map, bool[,] walkable, Vector2Int cell, out Vector2Int facing)
        {
            facing = default;
            int rockSides = 0;
            Vector2Int rockStep = default;
            foreach (var step in Steps)
            {
                if (map.IsRock(cell.x + step.x, cell.y + step.y))
                {
                    rockSides++;
                    rockStep = step;
                }
            }

            if (rockSides != 1)
            {
                return false;
            }

            int open = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if ((dx != 0 || dy != 0) && walkable[cell.x + dx, cell.y + dy])
                    {
                        open++;
                    }
                }
            }

            facing = -rockStep;
            return open >= 5;
        }

        // i passi di cammino dalla porta, nel pezzo che l'ingresso ancora raggiunge: -1 nel fondo e dove non si arriva
        private static int[,] Distances(bool[,] walkable, int width, int height, Vector2Int from, List<Vector2Int> region)
        {
            var distance = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    distance[x, y] = -1;
                }
            }

            var inside = new HashSet<Vector2Int>(region);
            var queue = new Queue<Vector2Int>();
            distance[from.x, from.y] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || distance[next.x, next.y] >= 0 || !walkable[next.x, next.y] || inside.Contains(next))
                    {
                        continue;
                    }

                    distance[next.x, next.y] = distance[cell.x, cell.y] + 1;
                    queue.Enqueue(next);
                }
            }

            return distance;
        }
    }
}

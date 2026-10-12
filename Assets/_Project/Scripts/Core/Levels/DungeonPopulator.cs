using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Il contenuto di una cripta generata (D6 della scheda M6): torce sui muri alti, casse e oggetti
    /// di scena contro i muri, scheletri a gruppi, più numerosi in profondità. Tutto come marcatori
    /// della mappa, gli stessi delle mappe a mano. Dalla M11 (D4) gli arredi: ogni stanza ha un tema e
    /// qualche composizione di mobili contro i muri, con un flusso suo del seme. Logica pura.
    /// </summary>
    public sealed class DungeonPopulator
    {
        public const char TorchSymbol = 'T';
        public const char EnemySymbol = 'S';
        public const char ChestSymbol = 'c';

        /// <summary>Uno scheletro mago (D6 della M9): da solo, in una stanza.</summary>
        public const char MageSymbol = 'm';

        // barile, casse di legno, pilastro: ostacoli, il NavMesh ci gira attorno
        private const string PropSymbols = "bxp";

        /// <summary>
        /// Le composizioni di mobili per tema (D4 della M11), tre per tema: deposito, biblioteca, cella,
        /// rovina. Ognuna occupa una cella contro un muro ed è un ostacolo.
        /// </summary>
        public static readonly string[] DecorThemes = { "DEF", "LNO", "RUV", "WYZ" };

        private const string DecorSymbols = "DEFLNORUVWYZ";

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly DungeonSettings _settings;

        public DungeonPopulator(DungeonSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Un simbolo che il cavaliere non attraversa: casse e oggetti di scena.</summary>
        public static bool IsObstacle(char symbol)
        {
            return symbol == ChestSymbol || PropSymbols.IndexOf(symbol) >= 0 || IsDecor(symbol) || CavePopulator.IsDecor(symbol);
        }

        /// <summary>Una composizione di mobili degli arredi della M11.</summary>
        public static bool IsDecor(char symbol)
        {
            return DecorSymbols.IndexOf(symbol) >= 0;
        }

        public void Populate(DungeonLayout layout, int depth, IRandomSource random)
        {
            var reserved = Reserved(layout);

            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                PlaceTorches(layout, layout.Rooms[i]);
            }

            PlaceChests(layout, depth, random, reserved);
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                int props = Range(random, 0, _settings.MaxPropsPerRoom);
                for (int p = 0; p < props; p++)
                {
                    TryPlaceAgainstWall(layout, layout.Rooms[i], PropSymbols[Range(random, 0, PropSymbols.Length - 1)], random, reserved);
                }
            }

            PlaceEnemies(layout, depth, random, reserved);

            // per ultimi: i tiri in più non spostano niente di quello che c'era prima (trappola 10 della M7)
            PlaceMages(layout, depth, random, reserved);
        }

        /// <summary>
        /// Gli arredi (D4 della M11), dopo tutto il resto e con un flusso suo del seme: nemici, casse e
        /// mappa dei semi già provati non cambiano. Ogni stanza ha un tema e una composizione ogni sei
        /// celle di lato, fino al massimo delle impostazioni, contro i muri e mai su un passaggio.
        /// </summary>
        public void Decorate(DungeonLayout layout, IRandomSource random)
        {
            var reserved = Reserved(layout);
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                RectInt room = layout.Rooms[i];
                string theme = DecorThemes[Range(random, 0, DecorThemes.Length - 1)];
                int count = Math.Min(_settings.MaxDecorPerRoom, 1 + (room.width + room.height) / 6);
                for (int d = 0; d < count; d++)
                {
                    TryPlaceAgainstWall(layout, room, theme[Range(random, 0, theme.Length - 1)], random, reserved);
                }
            }
        }

        // davanti alla scala si deve poter arrivare, ai suoi lati ci sono le balaustre, e il cavaliere
        // non deve comparire addosso a un barile: queste celle restano libere
        private static HashSet<Vector2Int> Reserved(DungeonLayout layout)
        {
            var reserved = new HashSet<Vector2Int>
            {
                layout.Stairs + Vector2Int.up, layout.Stairs + Vector2Int.left, layout.Stairs + Vector2Int.right,
            };
            foreach (var step in Steps)
            {
                reserved.Add(layout.Entrance + step);
            }

            return reserved;
        }

        // ogni mago in una stanza diversa se ce ne sono abbastanza, su una cella libera qualsiasi
        private void PlaceMages(DungeonLayout layout, int depth, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            Vector2Int range = _settings.MagesAt(depth);
            if (range.y <= 0)
            {
                return;
            }

            int count = Range(random, range.x, range.y);
            var rooms = OtherRooms(layout, random);
            for (int m = 0; m < count && rooms.Count > 0; m++)
            {
                RectInt room = layout.Rooms[rooms[m % rooms.Count]];
                var free = new List<Vector2Int>();
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    for (int x = room.xMin; x < room.xMax; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (layout[x, y] == LevelMap.Floor && !reserved.Contains(cell))
                        {
                            free.Add(cell);
                        }
                    }
                }

                if (free.Count > 0)
                {
                    var pick = free[Range(random, 0, free.Count - 1)];
                    layout.TryPlace(MageSymbol, pick.x, pick.y);
                }
            }
        }

        // Sul muro nord della stanza, dove oltre c'è roccia; se non c'è posto, sul muro est. Una ogni
        // CellsPerTorch celle di larghezza, distribuite: il builder le appende al muro alto della cella.
        private void PlaceTorches(DungeonLayout layout, RectInt room)
        {
            var candidates = new List<Vector2Int>();
            for (int x = room.xMin; x < room.xMax; x++)
            {
                if (!layout.IsFloor(x, room.yMin - 1) && layout[x, room.yMin] == LevelMap.Floor)
                {
                    candidates.Add(new Vector2Int(x, room.yMin));
                }
            }

            if (candidates.Count == 0)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    if (!layout.IsFloor(room.xMax, y) && layout[room.xMax - 1, y] == LevelMap.Floor)
                    {
                        candidates.Add(new Vector2Int(room.xMax - 1, y));
                    }
                }
            }

            int count = Math.Min(candidates.Count, 1 + (room.width - 1) / _settings.CellsPerTorch);
            for (int i = 0; i < count; i++)
            {
                var cell = candidates[(2 * i + 1) * candidates.Count / (2 * count)];
                layout.TryPlace(TorchSymbol, cell.x, cell.y);
            }
        }

        // Le casse nelle stanze lontane dall'ingresso, una per stanza finché ce ne sono.
        private void PlaceChests(DungeonLayout layout, int depth, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var rooms = OtherRooms(layout, random);
            int placed = 0;
            for (int i = 0; placed < _settings.ChestCount(depth) && i < rooms.Count * 2; i++)
            {
                if (TryPlaceAgainstWall(layout, layout.Rooms[rooms[i % rooms.Count]], ChestSymbol, random, reserved))
                {
                    placed++;
                }
            }
        }

        private void PlaceEnemies(DungeonLayout layout, int depth, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var rooms = OtherRooms(layout, random);
            int remaining = _settings.EnemyCount(depth);
            for (int i = 0; remaining > 0 && i < rooms.Count * 4; i++)
            {
                RectInt room = layout.Rooms[rooms[i % rooms.Count]];
                var free = new List<Vector2Int>();
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    for (int x = room.xMin; x < room.xMax; x++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (layout[x, y] == LevelMap.Floor && !reserved.Contains(cell))
                        {
                            free.Add(cell);
                        }
                    }
                }

                int group = Math.Min(remaining, Range(random, _settings.MinGroup, _settings.MaxGroup));
                for (int g = 0; g < group && free.Count > 0; g++)
                {
                    int pick = Range(random, 0, free.Count - 1);
                    layout.TryPlace(EnemySymbol, free[pick].x, free[pick].y);
                    free.RemoveAt(pick);
                    remaining--;
                }
            }
        }

        // Tutte le stanze tranne quella d'ingresso, in ordine mescolato dal seme.
        private static List<int> OtherRooms(DungeonLayout layout, IRandomSource random)
        {
            var rooms = new List<int>();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                if (i != layout.StartRoom)
                {
                    rooms.Add(i);
                }
            }

            for (int i = rooms.Count - 1; i > 0; i--)
            {
                int j = Range(random, 0, i);
                (rooms[i], rooms[j]) = (rooms[j], rooms[i]);
            }

            return rooms;
        }

        // Un ostacolo su una cella del bordo della stanza, mai su un passaggio verso un corridoio né
        // accanto: se comunque chiude il cammino verso un pezzo del livello, si toglie (trappola 5).
        private static bool TryPlaceAgainstWall(DungeonLayout layout, RectInt room, char symbol, IRandomSource random, HashSet<Vector2Int> reserved)
        {
            var candidates = new List<Vector2Int>();
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    bool border = x == room.xMin || y == room.yMin || x == room.xMax - 1 || y == room.yMax - 1;
                    var cell = new Vector2Int(x, y);
                    if (border && layout[x, y] == LevelMap.Floor && !reserved.Contains(cell) && !NearDoorway(layout, room, cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            while (candidates.Count > 0)
            {
                int pick = Range(random, 0, candidates.Count - 1);
                var cell = candidates[pick];
                candidates.RemoveAt(pick);
                layout.TryPlace(symbol, cell.x, cell.y);
                if (IsConnected(layout))
                {
                    return true;
                }

                layout.Remove(cell.x, cell.y);
            }

            return false;
        }

        // La cella, o una vicina dentro la stanza, confina con un pavimento fuori dalla stanza: un corridoio.
        private static bool NearDoorway(DungeonLayout layout, RectInt room, Vector2Int cell)
        {
            if (IsDoorway(layout, room, cell))
            {
                return true;
            }

            foreach (var step in Steps)
            {
                var next = cell + step;
                if (room.Contains(next) && IsDoorway(layout, room, next))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDoorway(DungeonLayout layout, RectInt room, Vector2Int cell)
        {
            foreach (var step in Steps)
            {
                var next = cell + step;
                if (!room.Contains(next) && layout.IsFloor(next.x, next.y))
                {
                    return true;
                }
            }

            return false;
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
            return c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol && !IsObstacle(c);
        }

        private static int Range(IRandomSource random, int min, int max)
        {
            return Math.Min(max, min + (int)(random.NextDouble() * (max - min + 1)));
        }
    }
}

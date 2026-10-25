using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I nemici speciali di un livello (M12), dopo il popolatore e con un flusso suo del seme: nemici,
    /// casse e arredi dei semi già provati restano dove sono. Un campione è un nemico già messo,
    /// promosso, con i suoi modificatori; l'unico è un simbolo suo in una cella libera. Ognuno ha una
    /// scorta di nemici normali in più, nelle celle libere attorno. Mai vicino all'ingresso né nella
    /// sala del re. Accanto all'unico e a metà dei campioni c'è un forziere; in fondo alla sala del re
    /// il suo tesoro (D8). Logica pura.
    /// </summary>
    public static class SpecialEnemyPlanner
    {
        /// <summary>L'unico del livello: il tileset dice quale (Morvak nella cripta, Grumm nelle caverne).</summary>
        public const char UniqueSymbol = 'u';

        /// <summary>Il forziere accanto all'unico e ai campioni (D8 della M12).</summary>
        public const char GoldChestSymbol = 'f';

        /// <summary>Il tesoro del re, in fondo alla sua sala (D8 della M12).</summary>
        public const char TreasureSymbol = 't';

        /// <summary>
        /// La direttiva con le celle delle scorte, "x,y" ciascuna (D13 della M13): i nemici di scorta lasciano
        /// un oggetto con metà della probabilità del loro tipo.
        /// </summary>
        public const string EscortDirective = "escorts";

        /// <summary>Di quante celle, oltre la scorta e una, il forziere si può allontanare dal suo capo.</summary>
        public const int GoldChestReach = 2;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        /// <summary>Un nemico che può diventare campione: tutti tranne il re.</summary>
        public static bool CanBeChampion(char symbol)
        {
            return symbol == DungeonPopulator.EnemySymbol || symbol == DungeonPopulator.MageSymbol
                || symbol == CavePopulator.SwarmSymbol || symbol == CavePopulator.BruteSymbol;
        }

        /// <summary>
        /// Mette nel livello l'unico, se <paramref name="withUnique"/>, e i gruppi di campioni, con le
        /// loro scorte; restituisce i campioni, per la direttiva <c>@champions</c>. Prima della
        /// profondità minima non cambia niente.
        /// </summary>
        public static List<ChampionSpawn> Place(DungeonLayout layout, int depth, SpecialEnemyRules rules, IReadOnlyList<string> modifierIds,
            bool withUnique, IRandomSource random, List<Vector2Int> escorts = null)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var champions = new List<ChampionSpawn>();
            if (depth < rules.MinDepth)
            {
                return champions;
            }

            var leaders = new List<Vector2Int>();
            bool uniquePlaced = false;
            if (withUnique)
            {
                var free = new List<Vector2Int>();
                foreach (var cell in Cells(layout))
                {
                    if (layout[cell.x, cell.y] == LevelMap.Floor && Allowed(layout, rules, cell)
                        && Chebyshev(cell, layout.Entrance) >= rules.EntranceDistance + 2)
                    {
                        free.Add(cell);
                    }
                }

                // dove la scorta più piccola ci sta tutta, se c'è un posto così: con gli arredi della M13
                // una cella stretta può lasciare l'unico da solo
                int needed = Math.Min(rules.Escort.x, rules.Escort.y);
                var roomy = free.FindAll(cell => EscortCells(layout, rules, cell).Count >= needed);
                if (roomy.Count > 0)
                {
                    free = roomy;
                }

                if (free.Count > 0)
                {
                    var pick = free[Range(random, 0, free.Count - 1)];
                    layout.TryPlace(UniqueSymbol, pick.x, pick.y);
                    leaders.Add(pick);
                    uniquePlaced = true;
                    AddEscort(layout, rules, pick, rules.UniqueEscort, rules.Escort, random, escorts);
                }
            }

            int groups = rules.Groups + (random.NextDouble() < rules.ExtraGroupChance ? 1 : 0);
            var candidates = new List<Vector2Int>();
            foreach (var cell in Cells(layout))
            {
                if (CanBeChampion(layout[cell.x, cell.y]) && Allowed(layout, rules, cell))
                {
                    candidates.Add(cell);
                }
            }

            Shuffle(candidates, random);
            foreach (var cell in candidates)
            {
                if (champions.Count >= groups)
                {
                    break;
                }

                if (NearAny(leaders, cell, rules.Spacing))
                {
                    continue;
                }

                char symbol = layout[cell.x, cell.y];
                champions.Add(new ChampionSpawn(cell.x, cell.y, PickModifiers(modifierIds, rules.ModifiersAt(depth), random)));
                leaders.Add(cell);
                AddEscort(layout, rules, cell, symbol, symbol == CavePopulator.SwarmSymbol ? rules.SwarmEscort : rules.Escort, random, escorts);
            }

            // i forzieri dopo tutti i gruppi: accanto all'unico sempre, a un campione con un tiro suo
            for (int i = 0; i < leaders.Count; i++)
            {
                if ((uniquePlaced && i == 0) || random.NextDouble() < rules.GoldChestChance)
                {
                    // vicino e nella sua stanza; se lì non c'è posto, un po' più in là anche fuori dalla stanza
                    for (int extra = 0; extra <= GoldChestReach && !PlaceGoldChest(layout, rules, leaders[i], rules.EscortRadius + 1 + extra, extra == 0, random); extra++)
                    {
                    }
                }
            }

            return champions;
        }

        /// <summary>
        /// Il tesoro del re (D8 della M12): nella sala del boss, contro la parete di fronte alla porta,
        /// dove si arriva solo passando dal re. Senza porta, contro la parete sud. Nessun tiro: la mappa
        /// dei semi già provati cambia solo di questa cella. False se il livello non ha la sala.
        /// </summary>
        public static bool PlaceKingsTreasure(DungeonLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (!layout.HasBossRoom)
            {
                return false;
            }

            // senza porta l'anello resta aperto dove la caverna toccava la sala: il tesoro sta dove dietro
            // c'è la roccia, o chiude il passaggio (ronda notturna del 25 ott 2026, seme 6764078822790321387).
            // Prima il centro della parete di fronte alla porta, poi accanto, poi le altre pareti
            var toGate = TowardGate(layout);
            foreach (var back in new[] { -toGate, new Vector2Int(toGate.y, toGate.x), new Vector2Int(-toGate.y, -toGate.x), toGate })
            {
                var side = new Vector2Int(back.y, back.x);
                foreach (int along in new[] { 0, 1, -1 })
                {
                    var cell = layout.BossCell + back * CaveGenerator.BossRoomHalf + side * along;
                    var behind = cell + back;
                    if (!layout.IsFloor(behind.x, behind.y) && layout.TryPlace(TreasureSymbol, cell.x, cell.y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Il passo, lungo un asse, dal centro della sala del re verso la sua porta; senza porta verso nord (y che cala).</summary>
        public static Vector2Int TowardGate(DungeonLayout layout)
        {
            var gate = layout.GateCell;
            if (gate.x < 0)
            {
                return Vector2Int.down;
            }

            var delta = gate - layout.BossCell;
            return Math.Abs(delta.x) >= Math.Abs(delta.y) ? new Vector2Int(Math.Sign(delta.x), 0) : new Vector2Int(0, Math.Sign(delta.y));
        }

        // Una cella vicina al capo, nella sua stanza se è nella cripta e sameRoom, con il davanti libero: prima contro
        // un muro, come le casse, che il builder gira verso la stanza; se non ce n'è, in mezzo alla caverna,
        // girata verso la camera. Se chiude un passaggio si prova la dopo.
        private static bool PlaceGoldChest(DungeonLayout layout, SpecialEnemyRules rules, Vector2Int leader, int radius, bool sameRoom, IRandomSource random)
        {
            RectInt? room = sameRoom ? RoomOf(layout, leader) : null;
            var againstWall = new List<Vector2Int>();
            var open = new List<Vector2Int>();
            for (int y = leader.y - radius; y <= leader.y + radius; y++)
            {
                for (int x = leader.x - radius; x <= leader.x + radius; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (layout[x, y] == LevelMap.Floor && Allowed(layout, rules, cell) && !NextToKeepClear(layout, cell)
                        && (room == null || room.Value.Contains(cell)) && Walkable(layout, cell + FrontOf(layout, cell, out bool wall)))
                    {
                        (wall ? againstWall : open).Add(cell);
                    }
                }
            }

            Shuffle(againstWall, random);
            Shuffle(open, random);
            againstWall.AddRange(open);
            int reachable = Reachable(layout);
            foreach (var cell in againstWall)
            {
                layout.TryPlace(GoldChestSymbol, cell.x, cell.y);
                if (Reachable(layout) == reachable - 1)
                {
                    return true;
                }

                layout.Remove(cell.x, cell.y);
            }

            return false;
        }

        /// <summary>
        /// Il passo verso il davanti di una cassa in questa cella, come la gira il builder: via dal primo
        /// muro (nord, sud, ovest, est); senza muri verso sud, la camera.
        /// </summary>
        public static Vector2Int FrontOf(DungeonLayout layout, Vector2Int cell, out bool againstWall)
        {
            foreach (var step in new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right })
            {
                if (!layout.IsFloor(cell.x + step.x, cell.y + step.y))
                {
                    againstWall = true;
                    return -step;
                }
            }

            againstWall = false;
            return Vector2Int.up;
        }

        private static bool Walkable(DungeonLayout layout, Vector2Int cell)
        {
            char c = layout[cell.x, cell.y];
            return c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(c);
        }

        // le celle dove si arriva a piedi dall'ingresso
        private static int Reachable(DungeonLayout layout)
        {
            var seen = new bool[layout.Width, layout.Height];
            var queue = new Queue<Vector2Int>();
            seen[layout.Entrance.x, layout.Entrance.y] = true;
            queue.Enqueue(layout.Entrance);
            int count = 1;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (Walkable(layout, next) && !seen[next.x, next.y])
                    {
                        seen[next.x, next.y] = true;
                        queue.Enqueue(next);
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// La profondità dove sta l'unico in questo mondo, o -1 se non c'è (D6 della M12): a ogni
        /// profondità, in ordine, una probabilità su tre, e la prima che riesce è l'unica. Così non
        /// compare due volte nello stesso mondo, e non serve salvarlo: lo dice il seme della partita.
        /// </summary>
        public static int UniqueDepth(ulong runSeed, int firstDepth, int lastDepth, float chance)
        {
            for (int depth = firstDepth; depth <= lastDepth; depth++)
            {
                if (new SplitMix64Source(SeedMixer.ForUnique(runSeed, depth)).NextDouble() < chance)
                {
                    return depth;
                }
            }

            return -1;
        }

        // lontano dall'ingresso, fuori dalla stanza di partenza della cripta e dalla sala del re con un giro attorno
        private static bool Allowed(DungeonLayout layout, SpecialEnemyRules rules, Vector2Int cell)
        {
            if (!layout.IsFloor(cell.x, cell.y) || Chebyshev(cell, layout.Entrance) < rules.EntranceDistance)
            {
                return false;
            }

            if (layout.StartRoom >= 0 && layout.Rooms[layout.StartRoom].Contains(cell))
            {
                return false;
            }

            return !layout.HasBossRoom || Chebyshev(cell, layout.BossCell) > CaveGenerator.BossRoomHalf + 2;
        }

        // Una cella libera vicina al capo, nella sua stanza se è nella cripta, e non davanti a una cassa,
        // alla scala o alla porta del re: lì si deve poter arrivare.
        private static void AddEscort(DungeonLayout layout, SpecialEnemyRules rules, Vector2Int leader, char symbol, Vector2Int range, IRandomSource random,
            List<Vector2Int> escorts)
        {
            var free = EscortCells(layout, rules, leader);
            int count = Range(random, Math.Min(range.x, range.y), Math.Max(range.x, range.y));
            Shuffle(free, random);
            for (int i = 0; i < count && i < free.Count; i++)
            {
                if (layout.TryPlace(symbol, free[i].x, free[i].y))
                {
                    escorts?.Add(free[i]);
                }
            }
        }

        /// <summary>Le celle delle scorte come gettoni della direttiva <see cref="EscortDirective"/>.</summary>
        public static string[] EscortTokens(IReadOnlyList<Vector2Int> escorts)
        {
            var tokens = new string[escorts.Count];
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = escorts[i].x + "," + escorts[i].y;
            }

            return tokens;
        }

        /// <summary>Le celle delle scorte scritte nella mappa; vuoto se non ce ne sono.</summary>
        public static HashSet<Vector2Int> EscortsFromMap(LevelMap map)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var token in map.GetDirective(EscortDirective))
            {
                var parts = token.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }

            return cells;
        }

        // Le celle libere attorno al capo dove può stare la scorta: nella sua stanza, se è in una stanza
        private static List<Vector2Int> EscortCells(DungeonLayout layout, SpecialEnemyRules rules, Vector2Int leader)
        {
            RectInt? room = RoomOf(layout, leader);
            var free = new List<Vector2Int>();
            int radius = rules.EscortRadius;
            for (int y = leader.y - radius; y <= leader.y + radius; y++)
            {
                for (int x = leader.x - radius; x <= leader.x + radius; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (layout[x, y] == LevelMap.Floor && Allowed(layout, rules, cell) && !NextToKeepClear(layout, cell)
                        && (room == null || room.Value.Contains(cell)))
                    {
                        free.Add(cell);
                    }
                }
            }

            return free;
        }

        private static RectInt? RoomOf(DungeonLayout layout, Vector2Int cell)
        {
            foreach (var room in layout.Rooms)
            {
                if (room.Contains(cell))
                {
                    return room;
                }
            }

            return null;
        }

        private static bool NextToKeepClear(DungeonLayout layout, Vector2Int cell)
        {
            foreach (var step in Steps)
            {
                char symbol = layout[cell.x + step.x, cell.y + step.y];
                if (symbol == DungeonPopulator.ChestSymbol || symbol == DungeonGenerator.StairsSymbol || symbol == DungeonGenerator.EntranceSymbol
                    || symbol == CavePopulator.GateSymbol || symbol == GoldChestSymbol || symbol == TreasureSymbol)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> PickModifiers(IReadOnlyList<string> ids, int wanted, IRandomSource random)
        {
            var picked = new List<string>();
            if (ids == null || ids.Count == 0)
            {
                return picked;
            }

            var pool = new List<string>(ids);
            for (int i = 0; i < wanted && pool.Count > 0; i++)
            {
                int index = Range(random, 0, pool.Count - 1);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return picked;
        }

        private static IEnumerable<Vector2Int> Cells(DungeonLayout layout)
        {
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        private static bool NearAny(List<Vector2Int> cells, Vector2Int cell, int distance)
        {
            foreach (var other in cells)
            {
                if (Chebyshev(other, cell) < distance)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        private static void Shuffle(List<Vector2Int> cells, IRandomSource random)
        {
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = Range(random, 0, i);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }
        }

        private static int Range(IRandomSource random, int min, int max)
        {
            return Math.Min(max, min + (int)(random.NextDouble() * (max - min + 1)));
        }
    }
}

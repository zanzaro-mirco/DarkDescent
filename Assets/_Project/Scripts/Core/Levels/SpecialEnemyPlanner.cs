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
    /// sala del re. Logica pura.
    /// </summary>
    public static class SpecialEnemyPlanner
    {
        /// <summary>L'unico del livello: il tileset dice quale (Morvak nella cripta, Grumm nelle caverne).</summary>
        public const char UniqueSymbol = 'u';

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
            bool withUnique, IRandomSource random)
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

                if (free.Count > 0)
                {
                    var pick = free[Range(random, 0, free.Count - 1)];
                    layout.TryPlace(UniqueSymbol, pick.x, pick.y);
                    leaders.Add(pick);
                    AddEscort(layout, rules, pick, rules.UniqueEscort, rules.Escort, random);
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
                AddEscort(layout, rules, cell, symbol, symbol == CavePopulator.SwarmSymbol ? rules.SwarmEscort : rules.Escort, random);
            }

            return champions;
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
        private static void AddEscort(DungeonLayout layout, SpecialEnemyRules rules, Vector2Int leader, char symbol, Vector2Int range, IRandomSource random)
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

            int count = Range(random, Math.Min(range.x, range.y), Math.Max(range.x, range.y));
            Shuffle(free, random);
            for (int i = 0; i < count && i < free.Count; i++)
            {
                layout.TryPlace(symbol, free[i].x, free[i].y);
            }
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
                    || symbol == CavePopulator.GateSymbol)
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

using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Le macchie a terra di un livello generato (D5 della M17): crepe, polvere, ossa, sangue secco,
    /// muschio, pozze, sassi e fuliggine sopra il pavimento, per rompere la ripetizione delle
    /// piastrelle. Da 1 a <see cref="MaxPerArea"/> per stanza della cripta (e qualcuna nei corridoi) o
    /// per zona delle caverne; ogni stanza o zona tira un tema, e il tema sceglie le caselle. Solo su
    /// celle di pavimento libere e lontano da scale, ingressi, cancello e punti d'interesse, così non
    /// coprono niente di quello che si guarda. Si tira con un seme suo (<see cref="SeedMixer.ForDecals"/>),
    /// dopo tutto il resto: la mappa, i nemici, le casse e gli arredi restano quelli di prima.
    /// Logica pura.
    /// </summary>
    public static class DecalPlanner
    {
        /// <summary>Le colonne e le righe dell'atlante delle macchie (D12): 1024 × 512, caselle da 256.</summary>
        public const int Columns = 4;

        public const int Rows = 2;

        // le caselle dell'atlante, nell'ordine di Tools/ComfyUI/images/decals.py
        public const int Cracks = 0;
        public const int Dust = 1;
        public const int Bones = 2;
        public const int Blood = 3;
        public const int Moss = 4;
        public const int Puddle = 5;
        public const int Pebbles = 6;
        public const int Soot = 7;

        public const int TileCount = Columns * Rows;

        /// <summary>Al più tante macchie per stanza o per zona.</summary>
        public const int MaxPerArea = 3;

        /// <summary>Nei corridoi al più una macchia ogni tante celle.</summary>
        public const int CorridorCellsPerDecal = 8;

        /// <summary>Il lato del quadrato, in decimetri.</summary>
        public const int MinSize = 16;

        public const int MaxSize = 30;

        /// <summary>Lo scarto massimo dal centro della cella, in decimetri per asse: con il lato più grande la parte visibile resta nella cella.</summary>
        public const int MaxOffset = 5;

        // tentativi per macchia: una cella occupata non fa ritirare all'infinito
        private const int Attempts = 4;

        /// <summary>I temi della cripta: deposito polveroso, battaglia, umido, bruciato.</summary>
        public static readonly int[][] CryptThemes =
        {
            new[] { Dust, Cracks, Pebbles },
            new[] { Blood, Bones, Cracks },
            new[] { Moss, Puddle, Cracks },
            new[] { Soot, Dust, Bones },
        };

        /// <summary>I temi delle caverne: umido, tana, frana.</summary>
        public static readonly int[][] CaveThemes =
        {
            new[] { Moss, Puddle, Pebbles },
            new[] { Bones, Blood, Pebbles },
            new[] { Cracks, Pebbles, Dust },
        };

        /// <summary>Le macchie del livello, nell'ordine delle stanze (o delle zone) e poi dei corridoi.</summary>
        public static List<DecalSpot> Plan(DungeonLayout layout, IReadOnlyList<int[]> themes, IRandomSource random)
        {
            var spots = new List<DecalSpot>();
            if (themes == null || themes.Count == 0)
            {
                return spots;
            }

            var clear = KeepClear(layout);
            var taken = new HashSet<Vector2Int>();
            foreach (var (cells, min, max) in Areas(layout))
            {
                if (cells.Count == 0)
                {
                    continue;
                }

                int[] theme = themes[Range(random, 0, themes.Count - 1)];
                int count = Range(random, min, Math.Max(min, max));
                for (int d = 0; d < count; d++)
                {
                    for (int attempt = 0; attempt < Attempts; attempt++)
                    {
                        var cell = cells[Range(random, 0, cells.Count - 1)];
                        if (layout[cell.x, cell.y] != LevelMap.Floor || clear.Contains(cell) || !taken.Add(cell))
                        {
                            continue;
                        }

                        spots.Add(new DecalSpot(cell.x, cell.y, theme[Range(random, 0, theme.Length - 1)], 15 * Range(random, 0, 23),
                            Range(random, -MaxOffset, MaxOffset), Range(random, -MaxOffset, MaxOffset), Range(random, MinSize, MaxSize)));
                        break;
                    }
                }
            }

            return spots;
        }

        // Le stanze della cripta e i suoi corridoi, o le zone quadrate delle caverne, che non hanno stanze
        private static IEnumerable<(List<Vector2Int> cells, int min, int max)> Areas(DungeonLayout layout)
        {
            if (layout.Rooms.Count == 0)
            {
                int zone = CavePopulator.DecorZone;
                for (int zy = 0; zy < layout.Height; zy += zone)
                {
                    for (int zx = 0; zx < layout.Width; zx += zone)
                    {
                        yield return (FloorCells(layout, new RectInt(zx, zy, zone, zone)), 1, MaxPerArea);
                    }
                }

                yield break;
            }

            foreach (var room in layout.Rooms)
            {
                yield return (FloorCells(layout, room), 1, MaxPerArea);
            }

            var corridors = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (layout.IsFloor(x, y) && !InAnyRoom(layout, x, y))
                    {
                        corridors.Add(new Vector2Int(x, y));
                    }
                }
            }

            yield return (corridors, 0, corridors.Count / CorridorCellsPerDecal);
        }

        private static List<Vector2Int> FloorCells(DungeonLayout layout, RectInt area)
        {
            var cells = new List<Vector2Int>();
            for (int y = area.y; y < area.yMax; y++)
            {
                for (int x = area.x; x < area.xMax; x++)
                {
                    if (layout.IsFloor(x, y))
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        private static bool InAnyRoom(DungeonLayout layout, int x, int y)
        {
            foreach (var room in layout.Rooms)
            {
                if (room.Contains(new Vector2Int(x, y)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Le celle senza macchie: attorno a scala, ingresso, cancello del re e ai punti d'interesse, i
        /// pezzi che si guardano e che occupano più della loro cella.
        /// </summary>
        public static HashSet<Vector2Int> KeepClear(DungeonLayout layout)
        {
            var centers = new List<Vector2Int> { layout.Stairs, layout.Entrance };
            if (layout.HasBossRoom)
            {
                centers.Add(layout.GateCell);
            }

            foreach (var feature in layout.Features)
            {
                centers.Add(feature.Cell);
            }

            var clear = new HashSet<Vector2Int>();
            foreach (var center in centers)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        clear.Add(new Vector2Int(center.x + dx, center.y + dy));
                    }
                }
            }

            return clear;
        }

        private static int Range(IRandomSource random, int min, int max)
        {
            return min + Math.Min(max - min, (int)(random.NextDouble() * (max - min + 1)));
        }
    }
}

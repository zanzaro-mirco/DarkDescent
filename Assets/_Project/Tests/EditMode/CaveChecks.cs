using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Levels;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// I controlli delle caverne e della sala del re, comuni ai test di ogni push e alla ronda notturna
    /// (passo 15.2): null se il livello va bene, altrimenti il primo errore.
    /// </summary>
    public static class CaveChecks
    {
        /// <summary>Il pavimento sta tra il 35 e il 45% dell'area dentro il bordo (D1 della M7).</summary>
        public const float LeastFloor = 0.35f;

        public const float MostFloor = 0.45f;

        /// <summary>
        /// Con la sala del re il pavimento scende, perché quello che l'anello taglia fuori torna roccia
        /// (la ronda del 23 ott 2026 ha visto il 30%), o sale, perché la sala scavata nella roccia
        /// aggiunge fino a 25 celle, il 4% (il 46% con il seme della notte 98765).
        /// </summary>
        public const float LeastBossFloor = 0.25f;

        public const float MostBossFloor = 0.50f;

        // nessuna cella chiusa, per Reaches
        private static readonly Vector2Int Nowhere = new Vector2Int(-1, -1);

        /// <summary>La parte di pavimento dentro il bordo di roccia.</summary>
        public static float FloorFraction(DungeonLayout layout)
        {
            int interior = (layout.Width - 2) * (layout.Height - 2);
            return MapChecks.Find(layout, c => c != LevelMap.Rock).Count / (float)interior;
        }

        /// <summary>Il pavimento è circa il 40% dell'area (meno con la sala del re), e il bordo resta roccia.</summary>
        public static string Shape(DungeonLayout layout)
        {
            string border = MapChecks.Border(layout);
            if (border != null)
            {
                return border;
            }

            float fraction = FloorFraction(layout);
            float least = layout.HasBossRoom ? LeastBossFloor : LeastFloor;
            float most = layout.HasBossRoom ? MostBossFloor : MostFloor;
            return fraction < least || fraction > most ? $"pavimento al {fraction:P0}\n{MapChecks.Text(layout)}" : null;
        }

        /// <summary>Niente pilastri di roccia di una cella in mezzo al pavimento, e niente pavimenti uniti solo in diagonale.</summary>
        public static string PillarsAndDiagonals(DungeonLayout layout)
        {
            for (int y = 1; y < layout.Height - 1; y++)
            {
                for (int x = 1; x < layout.Width - 1; x++)
                {
                    if (!layout.IsFloor(x, y) && layout.IsFloor(x - 1, y) && layout.IsFloor(x + 1, y) && layout.IsFloor(x, y - 1) && layout.IsFloor(x, y + 1))
                    {
                        return $"pilastro in ({x}, {y})\n{MapChecks.Text(layout)}";
                    }

                    bool a = layout.IsFloor(x, y), b = layout.IsFloor(x + 1, y), c = layout.IsFloor(x, y + 1), d = layout.IsFloor(x + 1, y + 1);
                    if ((a && d && !b && !c) || (b && c && !a && !d))
                    {
                        return $"pavimenti uniti solo in diagonale in ({x}, {y})\n{MapChecks.Text(layout)}";
                    }
                }
            }

            return null;
        }

        /// <summary>L'ingresso sta in uno spazio aperto: le otto celle attorno sono pavimento.</summary>
        public static string OpenEntrance(DungeonLayout layout)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (!layout.IsFloor(layout.Entrance.x + dx, layout.Entrance.y + dy))
                    {
                        return "l'ingresso non è in uno spazio aperto";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Scheletri, sciame, bruti e maghi negli intervalli della tabella, il boss solo alla sua
        /// profondità, le casse dei numeri, e nessuno vicino all'ingresso.
        /// </summary>
        public static string Population(DungeonLayout layout, CaveSettings settings, int depth)
        {
            var row = settings.SpawnTable.For(depth);
            var enemies = MapChecks.Find(layout, CavePopulator.IsEnemy);
            int swarm = enemies.Count(c => layout[c.x, c.y] == CavePopulator.SwarmSymbol);
            int brutes = enemies.Count(c => layout[c.x, c.y] == CavePopulator.BruteSymbol);
            int mages = enemies.Count(c => layout[c.x, c.y] == DungeonPopulator.MageSymbol);
            int bosses = enemies.Count(c => layout[c.x, c.y] == CavePopulator.BossSymbol);
            int skeletons = enemies.Count - swarm - brutes - mages - bosses;
            if (bosses != (depth == settings.BossDepth ? 1 : 0))
            {
                return $"{bosses} boss";
            }

            string range = Outside("bruti", brutes, row.Brutes.x, row.Brutes.y)
                ?? Outside("maghi", mages, row.Mages.x, row.Mages.y)
                ?? Outside("sciame", swarm, row.SwarmGroups.x * settings.MinSwarm, row.SwarmGroups.y * settings.MaxSwarm)
                ?? Outside("scheletri", skeletons, row.SkeletonGroups.x * settings.MinGroup, row.SkeletonGroups.y * settings.MaxGroup);
            if (range != null)
            {
                return range;
            }

            var chests = MapChecks.Find(layout, c => c == DungeonPopulator.ChestSymbol);
            if (chests.Count != settings.ChestCount(depth))
            {
                return $"{chests.Count} casse invece di {settings.ChestCount(depth)}";
            }

            // senza ostacoli: i passi della caverna com'è, non i giri attorno alle casse
            var bare = LevelGrid.Distances(Plain(layout), layout.Entrance);
            foreach (var cell in enemies.Concat(chests))
            {
                if (bare[cell.x, cell.y] < settings.QuietSteps)
                {
                    return $"{layout[cell.x, cell.y]} in {cell} troppo vicino all'ingresso";
                }
            }

            return null;
        }

        /// <summary>Nessun ostacolo chiude un cunicolo largo una cella.</summary>
        public static string Corridors(DungeonLayout layout)
        {
            foreach (var cell in MapChecks.Find(layout, DungeonPopulator.IsObstacle))
            {
                bool northSouth = !layout.IsFloor(cell.x, cell.y - 1) && !layout.IsFloor(cell.x, cell.y + 1);
                bool eastWest = !layout.IsFloor(cell.x - 1, cell.y) && !layout.IsFloor(cell.x + 1, cell.y);
                if (northSouth || eastWest)
                {
                    return $"ostacolo in un cunicolo in {cell}";
                }
            }

            return null;
        }

        /// <summary>
        /// Più di tre candele, contro la roccia e distanti tra loro. Quelle agli angoli della sala del re
        /// stanno contro l'anello, che manca quando la sala resta aperta.
        /// </summary>
        public static string Candles(DungeonLayout layout, CaveSettings settings)
        {
            var candles = MapChecks.Find(layout, c => c == CavePopulator.CandleSymbol);
            if (candles.Count <= 3)
            {
                return $"{candles.Count} candele";
            }

            for (int i = 0; i < candles.Count; i++)
            {
                var c = candles[i];
                bool inBossRoom = layout.HasBossRoom
                    && Math.Max(Math.Abs(c.x - layout.BossCell.x), Math.Abs(c.y - layout.BossCell.y)) <= CaveGenerator.BossRoomHalf;
                if (!inBossRoom && LevelGrid.Steps.All(s => layout.IsFloor(c.x + s.x, c.y + s.y)))
                {
                    return $"candela lontana dalla roccia in {c}";
                }

                for (int j = i + 1; j < candles.Count; j++)
                {
                    int apart = Math.Max(Math.Abs(c.x - candles[j].x), Math.Abs(c.y - candles[j].y));
                    if (apart < settings.CandleSpacing)
                    {
                        return $"candele troppo vicine in {c} e {candles[j]}";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// La sala del re, sulla mappa che il gioco costruisce: il re al centro di 5 × 5 celle libere con
        /// solo lui, nessuna scala, e dall'ingresso si arriva al re. Con la porta, la porta sta nell'anello
        /// attorno e si entra solo da lì; senza, la sala è rimasta aperta perché nessuna porta andava
        /// (CaveGenerator.TryCarveBossRoom), e il gioco la regge.
        /// </summary>
        public static string BossRoom(LevelMap map)
        {
            var kings = map.Markers.Where(m => m.Symbol == CavePopulator.BossSymbol).ToList();
            var gates = map.Markers.Where(m => m.Symbol == CavePopulator.GateSymbol).ToList();
            if (kings.Count != 1 || gates.Count > 1)
            {
                return $"{kings.Count} re e {gates.Count} porte";
            }

            if (map.Markers.Any(m => m.Symbol == DungeonGenerator.StairsSymbol))
            {
                return "l'ultima profondità ha la scala";
            }

            var king = kings[0];
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (!map.IsFloor(king.X + dx, king.Y + dy))
                    {
                        return $"la sala è piena in ({dx}, {dy})";
                    }
                }
            }

            if (map.Markers.Any(m => CavePopulator.IsEnemy(m.Symbol) && m.Symbol != CavePopulator.BossSymbol
                && Mathf.Max(Mathf.Abs(m.X - king.X), Mathf.Abs(m.Y - king.Y)) <= 3))
            {
                return "nella sala non c'è solo il re";
            }

            var entrance = map.Markers.Single(m => m.Symbol == DungeonGenerator.EntranceSymbol);
            if (!Reaches(map, entrance, king, Nowhere))
            {
                return "al re non si arriva";
            }

            if (gates.Count == 0)
            {
                return null;
            }

            var gate = new Vector2Int(gates[0].X, gates[0].Y);
            if (Mathf.Max(Mathf.Abs(gate.x - king.X), Mathf.Abs(gate.y - king.Y)) != 3)
            {
                return "la porta non sta nell'anello attorno alla sala";
            }

            return Reaches(map, entrance, king, gate) ? "chiusa la porta, si entra da un'altra parte" : null;
        }

        // le celle della caverna con i marcatori come pavimento; la scala resta
        private static char[,] Plain(DungeonLayout layout)
        {
            var cells = new char[layout.Width, layout.Height];
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    char c = layout[x, y];
                    cells[x, y] = c != LevelMap.Rock && c != DungeonGenerator.StairsSymbol ? LevelMap.Floor : c;
                }
            }

            return cells;
        }

        private static string Outside(string what, int count, int least, int most)
        {
            return count < least || count > most ? $"{count} {what}, fuori da {least}–{most}" : null;
        }

        // a piedi da una cella all'altra; la cella chiusa (la porta) non si attraversa
        private static bool Reaches(LevelMap map, MapMarker from, MapMarker to, Vector2Int closed)
        {
            var start = new Vector2Int(from.X, from.Y);
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell.x == to.X && cell.y == to.Y)
                {
                    return true;
                }

                foreach (var step in LevelGrid.Steps)
                {
                    var next = cell + step;
                    if (map.IsFloor(next.x, next.y) && next != closed && seen.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }
    }
}

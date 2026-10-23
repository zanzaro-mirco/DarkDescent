using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le verifiche comuni ai test dei generatori, scritte qui e non prese dai generatori: la mappa
    /// come testo per i messaggi d'errore, i passi a piedi dall'ingresso e i controlli che valgono per
    /// cripta e caverne. Ogni controllo restituisce null se il livello va bene, altrimenti il primo
    /// errore: i test di ogni push lo fanno fallire subito, la ronda notturna li raccoglie (passo 15.2).
    /// </summary>
    public static class MapChecks
    {
        /// <summary>
        /// La scala sta almeno a questa parte della distanza massima dall'ingresso. Era metà: una caverna
        /// con un corridoio lungo in fondo la lasciava a 22 passi su 46 (ronda notturna 31337, partita
        /// 10984456463612733930, profondità 7), e la scala va nella zona aperta, non in un corridoio.
        /// </summary>
        public const float LeastStairsShare = 0.45f;

        public static string Text(LevelMap map)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    sb.Append(map.GetSymbol(x, y));
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        public static string Text(DungeonLayout layout)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    sb.Append(layout[x, y]);
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>Passi a piedi in quattro direzioni; la scala non si attraversa.</summary>
        public static Dictionary<Vector2Int, int> Walk(LevelMap map, Vector2Int from)
        {
            var distances = new Dictionary<Vector2Int, int> { [from] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in LevelGrid.Steps)
                {
                    var next = cell + step;
                    if (map.IsFloor(next.x, next.y) && map.GetSymbol(next.x, next.y) != DungeonGenerator.StairsSymbol && !distances.ContainsKey(next))
                    {
                        distances[next] = distances[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distances;
        }

        /// <summary>Passi a piedi dall'ingresso sulle celle di pavimento che <paramref name="passable"/> accetta.</summary>
        public static Dictionary<Vector2Int, int> Walk(DungeonLayout layout, Func<char, bool> passable)
        {
            var distances = new Dictionary<Vector2Int, int> { [layout.Entrance] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.Entrance);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in LevelGrid.Steps)
                {
                    var next = cell + step;
                    if (layout.IsFloor(next.x, next.y) && passable(layout[next.x, next.y]) && !distances.ContainsKey(next))
                    {
                        distances[next] = distances[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return distances;
        }

        public static List<Vector2Int> Find(DungeonLayout layout, Func<char, bool> wanted)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    if (wanted(layout[x, y]))
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }

        public static int Count(DungeonLayout layout, char symbol)
        {
            return Find(layout, c => c == symbol).Count;
        }

        /// <summary>Nei test di ogni push: un errore fa fallire subito, con dove è successo.</summary>
        public static void Pass(string where, string error)
        {
            if (error != null)
            {
                Assert.Fail($"{where}: {error}");
            }
        }

        /// <summary>Ogni pavimento si raggiunge a piedi dall'ingresso e, se c'è la scala, davanti alla scala si arriva.</summary>
        public static string Reachable(DungeonLayout layout, bool stairs)
        {
            return Unreached(layout, c => c != DungeonGenerator.StairsSymbol, stairs, "irraggiungibile");
        }

        /// <summary>Lo stesso girando attorno a casse, arredi e scenografia: nessun ostacolo chiude un passaggio.</summary>
        public static string ObstaclesLeavePath(DungeonLayout layout, bool stairs)
        {
            return Unreached(layout, c => c != DungeonGenerator.StairsSymbol && !DungeonPopulator.IsObstacle(c), stairs, "chiusa da un ostacolo");
        }

        /// <summary>
        /// Un ingresso; se c'è la scala, una sola, con la roccia a nord (lo stendardo) e il pavimento a sud
        /// (da dove si arriva), ad almeno il 45% della distanza massima dall'ingresso (<see cref="LeastStairsShare"/>).
        /// </summary>
        public static string Exits(DungeonLayout layout, bool stairs)
        {
            int entrances = Count(layout, DungeonGenerator.EntranceSymbol);
            if (entrances != 1)
            {
                return $"{entrances} ingressi";
            }

            if (!stairs)
            {
                return null;
            }

            int count = Count(layout, DungeonGenerator.StairsSymbol);
            if (count != 1)
            {
                return $"{count} scale";
            }

            var at = layout.Stairs;
            if (layout.IsFloor(at.x, at.y - 1))
            {
                return "a nord della scala non c'è la roccia";
            }

            if (!layout.IsFloor(at.x, at.y + 1))
            {
                return "a sud della scala non c'è pavimento";
            }

            var distances = Walk(layout, c => c != DungeonGenerator.StairsSymbol);
            if (!distances.TryGetValue(at + Vector2Int.up, out int front))
            {
                return "davanti alla scala non si arriva";
            }

            int steps = front + 1;
            int farthest = distances.Values.Max();
            return steps < farthest * LeastStairsShare ? $"scala a {steps} passi, il punto più lontano a {farthest}\n{Text(layout)}" : null;
        }

        /// <summary>Il bordo della mappa resta roccia.</summary>
        public static string Border(DungeonLayout layout)
        {
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    bool edge = x == 0 || y == 0 || x == layout.Width - 1 || y == layout.Height - 1;
                    if (edge && layout.IsFloor(x, y))
                    {
                        return $"pavimento sul bordo in ({x}, {y})";
                    }
                }
            }

            return null;
        }

        // la scala non si attraversa mai; le celle che passable rifiuta non contano tra quelle da raggiungere
        private static string Unreached(DungeonLayout layout, Func<char, bool> passable, bool stairs, string problem)
        {
            var distances = Walk(layout, passable);
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    // il testo della mappa solo se serve: comporlo a ogni cella rallenta il test di secondi
                    if (layout.IsFloor(x, y) && passable(layout[x, y]) && !distances.ContainsKey(new Vector2Int(x, y)))
                    {
                        return $"({x}, {y}) {problem}\n{Text(layout)}";
                    }
                }
            }

            return stairs && !distances.ContainsKey(layout.Stairs + Vector2Int.up) ? "davanti alla scala non si arriva" : null;
        }
    }
}

using System.Collections.Generic;
using System.Text;
using DarkDescent.Levels;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le verifiche comuni ai test dei generatori, scritte qui e non prese dai generatori: la mappa
    /// come testo per i messaggi d'errore e i passi a piedi dall'ingresso.
    /// </summary>
    public static class MapChecks
    {
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

        /// <summary>Passi a piedi in quattro direzioni; la scala non si attraversa.</summary>
        public static Dictionary<Vector2Int, int> Walk(LevelMap map, Vector2Int from)
        {
            var distances = new Dictionary<Vector2Int, int> { [from] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
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
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Le visite sulla griglia dei generatori: passi a piedi in quattro direzioni, con la roccia e la
    /// scala che non si attraversano. Le usano la cripta e le caverne.
    /// </summary>
    public static class LevelGrid
    {
        public static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        /// <summary>Passi a piedi da una cella a tutte le altre; -1 dove non si arriva.</summary>
        public static int[,] Distances(char[,] cells, Vector2Int from)
        {
            int width = cells.GetLength(0), height = cells.GetLength(1);
            var distances = new int[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    distances[x, y] = -1;
                }
            }

            var queue = new Queue<Vector2Int>();
            distances[from.x, from.y] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height)
                    {
                        continue;
                    }

                    char c = cells[next.x, next.y];
                    if (c == LevelMap.Rock || c == DungeonGenerator.StairsSymbol || distances[next.x, next.y] >= 0)
                    {
                        continue;
                    }

                    distances[next.x, next.y] = distances[cell.x, cell.y] + 1;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        /// <summary>Ogni cella che non è roccia né scala si raggiunge a piedi da <paramref name="from"/>.</summary>
        public static bool AllFloorReachable(char[,] cells, Vector2Int from)
        {
            int[,] distances = Distances(cells, from);
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                for (int x = 0; x < cells.GetLength(0); x++)
                {
                    if (cells[x, y] != LevelMap.Rock && cells[x, y] != DungeonGenerator.StairsSymbol && distances[x, y] < 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

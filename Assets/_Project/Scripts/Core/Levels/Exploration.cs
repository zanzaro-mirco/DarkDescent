using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Le celle già viste di un livello (D15 della scheda M6). Si scoprono camminando: dalla cella
    /// del cavaliere, entro qualche passo sul pavimento, anche in diagonale: attorno a lui si scopre un
    /// quadrato, non una croce. Si va per passi e non per raggio, e la diagonale non taglia lo spigolo
    /// di un muro, così una stanza dietro un muro resta nascosta finché non ci si arriva. Logica pura.
    /// </summary>
    public sealed class Exploration
    {
        private static readonly Vector2Int[] Steps =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        private readonly LevelMap _map;
        private readonly bool[,] _explored;

        // riusate a ogni passo: camminare non deve creare liste nuove
        private readonly Queue<Vector2Int> _queue = new Queue<Vector2Int>();
        private readonly Dictionary<Vector2Int, int> _distance = new Dictionary<Vector2Int, int>();

        public Exploration(LevelMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _explored = new bool[map.Width, map.Height];
        }

        public LevelMap Map => _map;

        /// <summary>Le celle di pavimento scoperte finora.</summary>
        public int ExploredCount { get; private set; }

        /// <summary>Se la cella è pavimento già visto; fuori dalla mappa, no.</summary>
        public bool IsExplored(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _map.Width && y < _map.Height && _explored[x, y];
        }

        /// <summary>
        /// Riprende le celle viste da un'esplorazione dello stesso livello, ricostruito: dopo
        /// Ricomincia la cripta è la stessa, e la mappa scoperta resta. False, senza toccare niente,
        /// se le due mappe non hanno la stessa misura.
        /// </summary>
        public bool CopyExplored(Exploration other)
        {
            if (other == null || other._map.Width != _map.Width || other._map.Height != _map.Height)
            {
                return false;
            }

            Array.Copy(other._explored, _explored, _explored.Length);
            ExploredCount = other.ExploredCount;
            return true;
        }

        /// <summary>Le celle viste, una per bit, riga per riga: per il salvataggio (M8).</summary>
        public byte[] CellsToBytes()
        {
            var bytes = new byte[(_map.Width * _map.Height + 7) / 8];
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int i = y * _map.Width + x;
                    if (_explored[x, y])
                    {
                        bytes[i / 8] |= (byte)(1 << (i % 8));
                    }
                }
            }

            return bytes;
        }

        /// <summary>
        /// Riprende le celle viste da un salvataggio, per la stessa mappa. False, senza toccare
        /// niente, se i bit non bastano per la mappa. Solo il pavimento conta come scoperto.
        /// </summary>
        public bool LoadCells(byte[] bytes)
        {
            if (bytes == null || bytes.Length < (_map.Width * _map.Height + 7) / 8)
            {
                return false;
            }

            ExploredCount = 0;
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int i = y * _map.Width + x;
                    bool seen = (bytes[i / 8] & (1 << (i % 8))) != 0 && _map.IsFloor(x, y);
                    _explored[x, y] = seen;
                    if (seen)
                    {
                        ExploredCount++;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Scopre le celle di pavimento raggiungibili da <paramref name="from"/> in al più
        /// <paramref name="steps"/> passi. True se se n'è scoperta almeno una nuova.
        /// </summary>
        public bool Reveal(Vector2Int from, int steps)
        {
            if (!_map.IsFloor(from.x, from.y))
            {
                return false;
            }

            int before = ExploredCount;
            _queue.Clear();
            _distance.Clear();
            _queue.Enqueue(from);
            _distance[from] = 0;
            while (_queue.Count > 0)
            {
                var cell = _queue.Dequeue();
                if (!_explored[cell.x, cell.y])
                {
                    _explored[cell.x, cell.y] = true;
                    ExploredCount++;
                }

                int distance = _distance[cell];
                if (distance == steps)
                {
                    continue;
                }

                foreach (var step in Steps)
                {
                    var next = cell + step;
                    bool cutsCorner = step.x != 0 && step.y != 0
                        && (!_map.IsFloor(cell.x + step.x, cell.y) || !_map.IsFloor(cell.x, cell.y + step.y));
                    if (!cutsCorner && _map.IsFloor(next.x, next.y) && !_distance.ContainsKey(next))
                    {
                        _distance[next] = distance + 1;
                        _queue.Enqueue(next);
                    }
                }
            }

            return ExploredCount > before;
        }

        /// <summary>La cella che contiene un punto del mondo: i centri delle celle stanno sui multipli di <see cref="LevelMap.CellSize"/>.</summary>
        public static Vector2Int CellAt(Vector3 world)
        {
            return new Vector2Int(Mathf.RoundToInt(world.x / LevelMap.CellSize), Mathf.RoundToInt(-world.z / LevelMap.CellSize));
        }
    }
}

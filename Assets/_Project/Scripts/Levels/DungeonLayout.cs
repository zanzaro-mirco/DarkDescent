using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Un livello generato prima di diventare una <see cref="LevelMap"/>: la griglia con stanze e
    /// corridoi, le stanze, ingresso e scala. Il contenuto (nemici, casse, torce) si aggiunge come
    /// marcatori sopra il pavimento. Logica pura.
    /// </summary>
    public sealed class DungeonLayout
    {
        private readonly char[,] _cells;
        private readonly List<RectInt> _rooms;

        public DungeonLayout(char[,] cells, List<RectInt> rooms, int startRoom, int exitRoom, Vector2Int entrance, Vector2Int stairs)
        {
            _cells = cells;
            _rooms = rooms;
            StartRoom = startRoom;
            ExitRoom = exitRoom;
            Entrance = entrance;
            Stairs = stairs;
        }

        public int Width => _cells.GetLength(0);

        public int Height => _cells.GetLength(1);

        /// <summary>Le stanze, in celle: x è la colonna, y la riga (la riga 0 è il nord).</summary>
        public IReadOnlyList<RectInt> Rooms => _rooms;

        public int StartRoom { get; }

        public int ExitRoom { get; }

        public Vector2Int Entrance { get; }

        public Vector2Int Stairs { get; }

        public char this[int x, int y] => x >= 0 && y >= 0 && x < Width && y < Height ? _cells[x, y] : LevelMap.Rock;

        public bool IsFloor(int x, int y)
        {
            return this[x, y] != LevelMap.Rock;
        }

        /// <summary>Un marcatore su una cella di pavimento libera; false se la cella è roccia o già occupata.</summary>
        public bool TryPlace(char symbol, int x, int y)
        {
            if (this[x, y] != LevelMap.Floor)
            {
                return false;
            }

            _cells[x, y] = symbol;
            return true;
        }

        /// <summary>Toglie un marcatore messo con <see cref="TryPlace"/>: la cella torna pavimento.</summary>
        public void Remove(int x, int y)
        {
            if (IsFloor(x, y))
            {
                _cells[x, y] = LevelMap.Floor;
            }
        }

        /// <summary>La mappa da costruire, con le direttive (profondità, ingresso, uscita).</summary>
        public LevelMap ToMap(IReadOnlyDictionary<string, string[]> directives)
        {
            return LevelMap.FromCells(_cells, directives);
        }
    }
}

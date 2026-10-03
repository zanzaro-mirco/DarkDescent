using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Un livello come griglia di celle da 4 m, letto da una mappa di testo (D2 della scheda M3).
    /// Logica pura: la usa lo strumento di editor che costruisce le scene, e alla M6 il generatore
    /// produrrà la stessa griglia.
    /// </summary>
    /// <remarks>
    /// Formato: <c>#</c> e spazio sono roccia, <c>.</c> è pavimento, ogni altro carattere è un
    /// marcatore sopra un pavimento. Le righe che cominciano con <c>@</c> sono direttive
    /// (<c>@chiave valore…</c>), quelle con <c>//</c> commenti. La prima riga della griglia è il nord.
    /// </remarks>
    public sealed class LevelMap
    {
        public const float CellSize = 4f;

        private const char Rock = '#';
        private const char Empty = ' ';
        private const char Floor = '.';

        private readonly char[,] _cells;
        private readonly List<MapMarker> _markers = new List<MapMarker>();
        private readonly Dictionary<string, string[]> _directives = new Dictionary<string, string[]>();

        private LevelMap(char[,] cells)
        {
            _cells = cells;
        }

        public int Width => _cells.GetLength(0);

        public int Height => _cells.GetLength(1);

        public IReadOnlyList<MapMarker> Markers => _markers;

        public static LevelMap Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var rows = new List<string>();
            var directives = new List<string[]>();
            foreach (var rawLine in text.Replace("\r", string.Empty).Split('\n'))
            {
                if (rawLine.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                if (rawLine.StartsWith("@", StringComparison.Ordinal))
                {
                    directives.Add(rawLine.Substring(1).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                    continue;
                }

                rows.Add(rawLine.TrimEnd());
            }

            // le righe vuote in fondo al file non fanno parte della griglia
            while (rows.Count > 0 && rows[rows.Count - 1].Length == 0)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            int width = 0;
            foreach (var row in rows)
            {
                width = Math.Max(width, row.Length);
            }

            if (width == 0)
            {
                throw new FormatException("La mappa non ha celle.");
            }

            // righe più corte completate con roccia: un editor di testo toglie volentieri gli spazi in coda
            var cells = new char[width, rows.Count];
            var map = new LevelMap(cells);
            for (int y = 0; y < rows.Count; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = x < rows[y].Length ? rows[y][x] : Rock;
                    cells[x, y] = c == Empty ? Rock : c;
                    if (c != Rock && c != Empty && c != Floor)
                    {
                        map._markers.Add(new MapMarker(c, x, y));
                    }
                }
            }

            foreach (var directive in directives)
            {
                if (directive.Length == 0)
                {
                    continue;
                }

                var values = new string[directive.Length - 1];
                Array.Copy(directive, 1, values, 0, values.Length);
                map._directives[directive[0]] = values;
            }

            return map;
        }

        /// <summary>Fuori dalla griglia è tutto roccia: i bordi della mappa non vanno chiusi a mano.</summary>
        public bool IsFloor(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height && _cells[x, y] != Rock;
        }

        public char GetSymbol(int x, int y)
        {
            return IsFloor(x, y) ? _cells[x, y] : Rock;
        }

        /// <summary>I valori di una direttiva <c>@chiave</c>; vuoto se la mappa non ce l'ha.</summary>
        public IReadOnlyList<string> GetDirective(string key)
        {
            return _directives.TryGetValue(key, out var values) ? values : Array.Empty<string>();
        }

        /// <summary>
        /// Ogni lato di pavimento che confina con la roccia: lì va un muro. Ordine stabile, riga per
        /// riga, per avere scene uguali a ogni ricostruzione.
        /// </summary>
        public IEnumerable<(int X, int Y, MapDirection Side)> BoundaryEdges()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!IsFloor(x, y))
                    {
                        continue;
                    }

                    if (!IsFloor(x, y - 1)) yield return (x, y, MapDirection.North);
                    if (!IsFloor(x + 1, y)) yield return (x, y, MapDirection.East);
                    if (!IsFloor(x, y + 1)) yield return (x, y, MapDirection.South);
                    if (!IsFloor(x - 1, y)) yield return (x, y, MapDirection.West);
                }
            }
        }

        /// <summary>Centro della cella nel mondo, al livello del pavimento. Le righe scendono verso -Z.</summary>
        public static Vector3 CellCenter(int x, int y)
        {
            return new Vector3(x * CellSize, 0f, -y * CellSize);
        }

        /// <summary>Verso del lato, in coordinate del mondo.</summary>
        public static Vector3 ToWorld(MapDirection side)
        {
            switch (side)
            {
                case MapDirection.North: return Vector3.forward;
                case MapDirection.East: return Vector3.right;
                case MapDirection.South: return Vector3.back;
                default: return Vector3.left;
            }
        }
    }
}

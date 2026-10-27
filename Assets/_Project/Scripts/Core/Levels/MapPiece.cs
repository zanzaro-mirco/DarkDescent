using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I pezzi della mappa (M18): un simbolo di <c>@solid</c> con le celle coperte (<c>%</c>) attaccate
    /// a lui. Il pezzo sta al centro delle sue celle e guarda dal lato in cui sta il suo simbolo: in
    /// una casa di 2×2 il simbolo in alto a destra apre la porta a est o a nord, verso il pavimento.
    /// Logica pura.
    /// </summary>
    public static class MapPiece
    {
        // da dove si preferisce entrare, come per la scala: da sud la porta guarda la camera di partenza
        private static readonly MapDirection[] FacingOrder = { MapDirection.South, MapDirection.West, MapDirection.East, MapDirection.North };

        private static readonly Vector2Int[] Steps = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        /// <summary>Le celle del pezzo: la sua e le coperte raggiungibili da lì senza passare da altro, in ordine di lettura.</summary>
        public static List<Vector2Int> Footprint(LevelMap map, MapMarker anchor)
        {
            var start = new Vector2Int(anchor.X, anchor.Y);
            var cells = new List<Vector2Int> { start };
            var seen = new HashSet<Vector2Int> { start };
            for (int i = 0; i < cells.Count; i++)
            {
                foreach (var step in Steps)
                {
                    var next = cells[i] + step;
                    if (map.IsCovered(next.x, next.y) && seen.Add(next))
                    {
                        cells.Add(next);
                    }
                }
            }

            cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return cells;
        }

        /// <summary>Il centro delle celle del pezzo nel mondo, al livello del pavimento.</summary>
        public static Vector3 Center(IReadOnlyList<Vector2Int> cells)
        {
            var sum = Vector3.zero;
            foreach (var cell in cells)
            {
                sum += LevelMap.CellCenter(cell.x, cell.y);
            }

            return sum / cells.Count;
        }

        /// <summary>
        /// Il lato del simbolo che tocca un pavimento e si allontana di più dal centro delle celle; a pari
        /// merito, e nei pezzi di una cella sola, prima sud, poi ovest, est e nord. False se non tocca
        /// nessun pavimento: un pezzo del paesaggio.
        /// </summary>
        public static bool TryFacing(LevelMap map, MapMarker anchor, IReadOnlyList<Vector2Int> cells, out MapDirection side)
        {
            var center = Vector2.zero;
            foreach (var cell in cells)
            {
                center += cell;
            }

            Vector2 outward = new Vector2(anchor.X, anchor.Y) - center / cells.Count;
            bool found = false;
            float best = float.NegativeInfinity;
            side = MapDirection.South;
            foreach (var candidate in FacingOrder)
            {
                var step = Steps[(int)candidate];
                float score = Vector2.Dot(step, outward);
                if (map.IsFloor(anchor.X + step.x, anchor.Y + step.y) && score > best + 0.001f)
                {
                    best = score;
                    side = candidate;
                    found = true;
                }
            }

            return found;
        }
    }
}

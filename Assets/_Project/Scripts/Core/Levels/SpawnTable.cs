using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La tabella dei nemici per profondità (D10 della M7), nelle impostazioni del livello: una riga
    /// vale dalla sua profondità in giù. Intervalli e non pesi: il primo bruto deve arrivare al 5,
    /// non una volta su tre. I numeri si ritarano qui, provando la build (M10).
    /// </summary>
    [Serializable]
    public class SpawnTable
    {
        [SerializeField] private List<SpawnRow> _rows = new List<SpawnRow>();

        public SpawnTable()
        {
        }

        public SpawnTable(IEnumerable<SpawnRow> rows)
        {
            _rows.AddRange(rows);
        }

        public IReadOnlyList<SpawnRow> Rows => _rows;

        /// <summary>
        /// I numeri di D10: al 5 circa cinque scheletri, uno sciame e un bruto; all'8 circa quattro
        /// scheletri, tre sciami e tre bruti.
        /// </summary>
        public static SpawnTable Caves()
        {
            return new SpawnTable(new[]
            {
                new SpawnRow(5, new Vector2Int(2, 2), new Vector2Int(1, 1), new Vector2Int(1, 1)),
                new SpawnRow(6, new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(1, 2)),
                new SpawnRow(7, new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(2, 2)),
                new SpawnRow(8, new Vector2Int(1, 2), new Vector2Int(3, 3), new Vector2Int(3, 3)),
            });
        }

        /// <summary>La riga più profonda tra quelle che non superano la profondità; la prima se nessuna.</summary>
        public SpawnRow For(int depth)
        {
            if (_rows.Count == 0)
            {
                throw new InvalidOperationException("La tabella dei nemici è vuota.");
            }

            SpawnRow chosen = _rows[0];
            foreach (var row in _rows)
            {
                if (row.Depth <= depth && (chosen.Depth > depth || row.Depth > chosen.Depth))
                {
                    chosen = row;
                }
            }

            return chosen;
        }
    }
}

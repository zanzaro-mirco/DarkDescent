using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Dove va lo sbocco del cunicolo alla profondità 1 (D7 della M18): sempre in un muro della stanza
    /// d'ingresso, al posto di quel pezzo di muro, con l'apertura verso la stanza (chiesto da Mirco il
    /// 28 ott 2026: mai in mezzo alla stanza). Prima il muro nord, poi l'ovest, che la camera di partenza
    /// (da sud-ovest) vede alti; est e sud, che vede bassi, solo se quelli non vanno. Su ogni muro la cella più
    /// vicina all'ingresso. Una cella va bene se è pavimento libero o ha solo una torcia (il builder
    /// non la mette: lo sbocco ha i suoi bracieri), se non è d'angolo, dove i drappi e i bracieri
    /// entrerebbero nel muro di lato, e se dietro c'è roccia, dove entra la galleria. Accanto può
    /// cominciare un corridoio: il fronte di massi ci sporge per meno di un metro, dentro il suo muro.
    /// Dipende solo dalla disposizione, senza tiri del seme. Logica pura.
    /// </summary>
    public static class TunnelSpot
    {
        private static readonly MapDirection[] Walls = { MapDirection.North, MapDirection.West, MapDirection.East, MapDirection.South };

        /// <summary>La cella dello sbocco e il muro che prende; false se nessun muro va bene, e il livello resta senza.</summary>
        public static bool TryChoose(DungeonLayout layout, out Vector2Int cell, out MapDirection wall)
        {
            var room = layout.Rooms[layout.StartRoom];
            foreach (var side in Walls)
            {
                Vector2Int back = Step(side);
                var along = new Vector2Int(Mathf.Abs(back.y), Mathf.Abs(back.x));
                int best = int.MaxValue;
                cell = layout.Entrance;
                foreach (var candidate in Row(room, side))
                {
                    var offset = candidate - layout.Entrance;
                    int distance = Mathf.Abs(offset.x * along.x + offset.y * along.y);
                    if (distance < best && Fits(layout, candidate, back, along))
                    {
                        best = distance;
                        cell = candidate;
                    }
                }

                if (best != int.MaxValue)
                {
                    wall = side;
                    return true;
                }
            }

            cell = layout.Entrance;
            wall = MapDirection.North;
            return false;
        }

        /// <summary>La direttiva d'arrivo con il tipo, la colonna, la riga e il muro.</summary>
        public static string[] ToTokens(string arrival, Vector2Int cell, MapDirection wall)
        {
            return new[] { arrival, cell.x.ToString(CultureInfo.InvariantCulture), cell.y.ToString(CultureInfo.InvariantCulture), wall.ToString() };
        }

        /// <summary>Cella e muro dalla direttiva scritta da <see cref="ToTokens"/>; false se mancano o non si leggono.</summary>
        public static bool TryRead(IReadOnlyList<string> tokens, out Vector2Int cell, out MapDirection wall)
        {
            cell = default;
            wall = MapDirection.North;
            if (tokens.Count < 4
                || !int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                || !int.TryParse(tokens[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                || !System.Enum.TryParse(tokens[3], out wall))
            {
                return false;
            }

            cell = new Vector2Int(x, y);
            return true;
        }

        // la fila di celle della stanza contro quel muro, in ordine: a parità di distanza vince la prima
        private static IEnumerable<Vector2Int> Row(RectInt room, MapDirection side)
        {
            bool horizontal = side == MapDirection.North || side == MapDirection.South;
            int fixedAt = side == MapDirection.North ? room.yMin
                : side == MapDirection.South ? room.yMax - 1
                : side == MapDirection.West ? room.xMin
                : room.xMax - 1;
            int from = horizontal ? room.xMin : room.yMin;
            int to = horizontal ? room.xMax : room.yMax;
            for (int i = from; i < to; i++)
            {
                yield return horizontal ? new Vector2Int(i, fixedAt) : new Vector2Int(fixedAt, i);
            }
        }

        private static bool Fits(DungeonLayout layout, Vector2Int cell, Vector2Int back, Vector2Int along)
        {
            char symbol = layout[cell.x, cell.y];
            if ((symbol != LevelMap.Floor && symbol != DungeonPopulator.TorchSymbol)
                || !layout.IsFloor(cell.x + along.x, cell.y + along.y) || !layout.IsFloor(cell.x - along.x, cell.y - along.y))
            {
                return false;
            }

            foreach (var feature in layout.Features)
            {
                if (feature.Cell == cell)
                {
                    return false;
                }
            }

            var behind = cell + back;
            return layout[behind.x, behind.y] == LevelMap.Rock;
        }

        // le righe della mappa scendono verso sud
        private static Vector2Int Step(MapDirection side)
        {
            switch (side)
            {
                case MapDirection.North: return new Vector2Int(0, -1);
                case MapDirection.East: return new Vector2Int(1, 0);
                case MapDirection.South: return new Vector2Int(0, 1);
                default: return new Vector2Int(-1, 0);
            }
        }
    }
}

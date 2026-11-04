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
    /// non la mette: lo sbocco ha i suoi bracieri), se le due celle di lato sono della stanza, e se
    /// dietro c'è roccia, dove entra la galleria. Di lato non va un angolo, dove i drappi e i bracieri
    /// entrerebbero nel muro, né l'inizio di un corridoio che parte lungo lo stesso muro: il fronte di
    /// massi ne stringe l'imbocco e il NavMesh non passa più (ronde del 31 ott e del 1 nov 2026). Un
    /// corridoio che parte dal muro dello sbocco, accanto, va bene: il fronte ci sporge per meno di un
    /// metro, dentro il suo muro. Dipende solo dalla disposizione, senza tiri del seme. Logica pura.
    /// </summary>
    public static class TunnelSpot
    {
        private static readonly MapDirection[] Walls = { MapDirection.North, MapDirection.West, MapDirection.East, MapDirection.South };

        /// <summary>La cella dello sbocco e il muro che prende; false se nessun muro va bene, e il livello resta senza.</summary>
        public static bool TryChoose(DungeonLayout layout, out Vector2Int cell, out MapDirection wall)
        {
            // le caverne non hanno una stanza d'ingresso (M22): si cerca attorno all'ingresso
            if (layout.StartRoom < 0 || layout.StartRoom >= layout.Rooms.Count)
            {
                return TryChooseInCave(layout, out cell, out wall);
            }

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
                    if (distance < best && Fits(layout, room, candidate, back, along))
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

        /// <summary>Quanti passi dall'ingresso, al più, si cerca il posto nelle caverne.</summary>
        public const int CaveSearchRadius = 14;

        // Nelle caverne: la cella libera più vicina all'ingresso a piedi (a parità, il muro nord, poi l'ovest, l'est, il sud) con un
        // muro dritto dietro, le due celle di lato libere e il quadrato davanti libero: la stessa forma di un tratto di muro di una
        // stanza, senza la stanza. Mai a meno di due celle dall'ingresso, perché il trigger sporge oltre la cella davanti.
        private static bool TryChooseInCave(DungeonLayout layout, out Vector2Int cell, out MapDirection wall)
        {
            var steps = new Dictionary<Vector2Int, int> { [layout.Entrance] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(layout.Entrance);
            var order = new List<Vector2Int>();
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                order.Add(current);
                if (steps[current] >= CaveSearchRadius)
                {
                    continue;
                }

                foreach (var step in LevelGrid.Steps)
                {
                    var next = current + step;
                    char symbol = layout[next.x, next.y];
                    if (layout.IsFloor(next.x, next.y) && symbol != DungeonGenerator.StairsSymbol && !steps.ContainsKey(next))
                    {
                        steps[next] = steps[current] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            // Come nelle stanze: prima il muro nord, poi l'ovest, che la camera vede alti, poi est e sud. Su ogni muro il tratto più
            // vicino (l'ordine della visita è per distanza, senza tiri), prima quello che somiglia a un muro di stanza e poi, se le pareti
            // storte della caverna non ne hanno, con meno pretese.
            foreach (var side in Walls)
            {
                Vector2Int back = Step(side);
                var along = new Vector2Int(Mathf.Abs(back.y), Mathf.Abs(back.x));
                for (int strictness = 0; strictness < CavePasses; strictness++)
                {
                    foreach (var candidate in order)
                    {
                        if (Mathf.Max(Mathf.Abs(candidate.x - layout.Entrance.x), Mathf.Abs(candidate.y - layout.Entrance.y)) >= 2
                            && layout[candidate.x, candidate.y] == LevelMap.Floor
                            && FitsInCave(layout, candidate, back, along, strictness))
                        {
                            cell = candidate;
                            wall = side;
                            return true;
                        }
                    }
                }
            }

            cell = layout.Entrance;
            wall = MapDirection.North;
            return false;
        }

        private const int CavePasses = 3;

        // 0: un tratto di muro dritto con le celle di lato e il quadrato davanti liberi; 1: le celle di lato possono essere
        // roccia (una nicchia nel muro); 2: basta roccia dietro e una cella libera davanti
        private static bool FitsInCave(DungeonLayout layout, Vector2Int cell, Vector2Int back, Vector2Int along, int strictness)
        {
            var behind = cell + back;
            var front = cell - back;
            bool Free(Vector2Int c) => layout[c.x, c.y] == LevelMap.Floor;
            bool Rock(Vector2Int c) => layout[c.x, c.y] == LevelMap.Rock;
            if (!Rock(behind) || !Free(front))
            {
                return false;
            }

            if (strictness < 2 && (!Rock(behind + along) || !Rock(behind - along)))
            {
                return false;
            }

            if (strictness < 1 && (!Free(cell + along) || !Free(cell - along) || !Free(front + along) || !Free(front - along)))
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

            return layout.StaysConnectedWithout(cell);
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

        private static bool Fits(DungeonLayout layout, RectInt room, Vector2Int cell, Vector2Int back, Vector2Int along)
        {
            char symbol = layout[cell.x, cell.y];
            if ((symbol != LevelMap.Floor && symbol != DungeonPopulator.TorchSymbol)
                || !room.Contains(cell + along) || !room.Contains(cell - along)
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
            return layout[behind.x, behind.y] == LevelMap.Rock && layout.StaysConnectedWithout(cell);
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

using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La cripta generata (D2 della scheda M6): BSP sulla griglia di celle da 4 m, una stanza per
    /// zona, corridoi tra le stanze più vicine dei due rami di ogni divisione. Collegando così ogni
    /// coppia di rami, il livello è connesso per costruzione. Ingresso in una stanza scelta dal seme,
    /// scala contro il muro nord della stanza più lontana a piedi. Logica pura e solo interi:
    /// stesso seme, stessa cripta, anche in build Web (trappola 8).
    /// </summary>
    public sealed class DungeonGenerator
    {
        public const char EntranceSymbol = '<';
        public const char StairsSymbol = '>';

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly DungeonSettings _settings;

        // lo stato di una generazione: rifatto da capo a ogni Generate
        private IRandomSource _random;
        private char[,] _cells;
        private List<RectInt> _rooms;

        public DungeonGenerator(DungeonSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public DungeonLayout Generate(ulong seed)
        {
            _random = new SplitMix64Source(seed);
            _cells = new char[_settings.Width, _settings.Height];
            for (int y = 0; y < _settings.Height; y++)
            {
                for (int x = 0; x < _settings.Width; x++)
                {
                    _cells[x, y] = LevelMap.Rock;
                }
            }

            // il bordo resta roccia: ogni pavimento ha un muro attorno
            _rooms = new List<RectInt>();
            Split(new RectInt(1, 1, _settings.Width - 2, _settings.Height - 2));

            int start = Range(0, _rooms.Count - 1);
            Vector2Int entrance = Center(_rooms[start]);
            int exit = FarthestRoom(entrance, start);
            Vector2Int stairs = PlaceStairs(_rooms[exit], entrance);
            _cells[entrance.x, entrance.y] = EntranceSymbol;

            return new DungeonLayout(_cells, _rooms, start, exit, entrance, stairs);
        }

        // Divide la zona finché è più grande di MaxLeaf; restituisce le stanze del ramo, già collegate tra loro.
        private List<int> Split(RectInt area)
        {
            int minLeaf = _settings.MinLeaf;
            bool canSplitX = area.width >= 2 * minLeaf;
            bool canSplitY = area.height >= 2 * minLeaf;
            bool tooBig = area.width > _settings.MaxLeaf || area.height > _settings.MaxLeaf;
            if (!tooBig || (!canSplitX && !canSplitY))
            {
                return new List<int> { AddRoom(area) };
            }

            // una zona molto più larga che alta si taglia in verticale, e viceversa: niente stanze a striscia
            bool splitX;
            if (!canSplitY || (canSplitX && area.width * 4 > area.height * 5))
            {
                splitX = true;
            }
            else if (!canSplitX || area.height * 4 > area.width * 5)
            {
                splitX = false;
            }
            else
            {
                splitX = _random.NextDouble() < 0.5;
            }

            RectInt first, second;
            if (splitX)
            {
                int cut = Range(minLeaf, area.width - minLeaf);
                first = new RectInt(area.x, area.y, cut, area.height);
                second = new RectInt(area.x + cut, area.y, area.width - cut, area.height);
            }
            else
            {
                int cut = Range(minLeaf, area.height - minLeaf);
                first = new RectInt(area.x, area.y, area.width, cut);
                second = new RectInt(area.x, area.y + cut, area.width, area.height - cut);
            }

            var rooms = Split(first);
            var others = Split(second);
            Connect(rooms, others);
            rooms.AddRange(others);
            return rooms;
        }

        // Una stanza dentro la zona, con almeno una cella di roccia per parte: due stanze non si toccano mai.
        private int AddRoom(RectInt leaf)
        {
            int width = Range(_settings.MinRoom, Math.Max(_settings.MinRoom, Math.Min(_settings.MaxRoom, leaf.width - 2)));
            int height = Range(_settings.MinRoom, Math.Max(_settings.MinRoom, Math.Min(_settings.MaxRoom, leaf.height - 2)));
            int x = leaf.x + 1 + Range(0, Math.Max(0, leaf.width - 2 - width));
            int y = leaf.y + 1 + Range(0, Math.Max(0, leaf.height - 2 - height));
            var room = new RectInt(x, y, width, height);
            for (int cy = room.yMin; cy < room.yMax; cy++)
            {
                for (int cx = room.xMin; cx < room.xMax; cx++)
                {
                    _cells[cx, cy] = LevelMap.Floor;
                }
            }

            _rooms.Add(room);
            return _rooms.Count - 1;
        }

        // Corridoio a L, largo una cella, tra le due stanze più vicine dei due rami.
        private void Connect(List<int> first, List<int> second)
        {
            Vector2Int from = default, to = default;
            int best = int.MaxValue;
            foreach (int a in first)
            {
                foreach (int b in second)
                {
                    Vector2Int ca = Center(_rooms[a]);
                    Vector2Int cb = Center(_rooms[b]);
                    int distance = Math.Abs(ca.x - cb.x) + Math.Abs(ca.y - cb.y);
                    if (distance < best)
                    {
                        best = distance;
                        from = ca;
                        to = cb;
                    }
                }
            }

            if (_random.NextDouble() < 0.5)
            {
                CarveRow(from.y, from.x, to.x);
                CarveColumn(to.x, from.y, to.y);
            }
            else
            {
                CarveColumn(from.x, from.y, to.y);
                CarveRow(to.y, from.x, to.x);
            }
        }

        private void CarveRow(int y, int x0, int x1)
        {
            for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++)
            {
                _cells[x, y] = LevelMap.Floor;
            }
        }

        private void CarveColumn(int x, int y0, int y1)
        {
            for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++)
            {
                _cells[x, y] = LevelMap.Floor;
            }
        }

        private int FarthestRoom(Vector2Int entrance, int start)
        {
            int[,] distances = Distances(entrance);
            int farthest = start == 0 ? 1 : 0;
            for (int i = 0; i < _rooms.Count; i++)
            {
                Vector2Int c = Center(_rooms[i]);
                Vector2Int f = Center(_rooms[farthest]);
                if (i != start && distances[c.x, c.y] > distances[f.x, f.y])
                {
                    farthest = i;
                }
            }

            return farthest;
        }

        // Sulla riga nord della stanza, con la roccia oltre (lì va lo stendardo) e il pavimento davanti
        // (da lì si arriva), il più vicino possibile al centro. La cella della scala non è pavimento:
        // si scarta quella che taglierebbe fuori una parte del livello.
        private Vector2Int PlaceStairs(RectInt room, Vector2Int entrance)
        {
            int centerX = Center(room).x;
            var candidates = new List<Vector2Int>();
            for (int y = room.yMin; y < room.yMax - 1; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    if (_cells[x, y - 1] == LevelMap.Rock && _cells[x, y + 1] == LevelMap.Floor)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            // prima la riga più a nord, poi il più vicino al centro: ordine stabile, niente sort instabili
            candidates.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y)
                : Math.Abs(a.x - centerX) != Math.Abs(b.x - centerX) ? Math.Abs(a.x - centerX).CompareTo(Math.Abs(b.x - centerX))
                : a.x.CompareTo(b.x));
            foreach (var cell in candidates)
            {
                _cells[cell.x, cell.y] = StairsSymbol;
                if (AllFloorReachable(entrance))
                {
                    return cell;
                }

                _cells[cell.x, cell.y] = LevelMap.Floor;
            }

            throw new InvalidOperationException($"Nessun posto per la scala nella stanza {room}.");
        }

        private bool AllFloorReachable(Vector2Int from)
        {
            int[,] distances = Distances(from);
            for (int y = 0; y < _settings.Height; y++)
            {
                for (int x = 0; x < _settings.Width; x++)
                {
                    if (_cells[x, y] != LevelMap.Rock && _cells[x, y] != StairsSymbol && distances[x, y] < 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Passi a piedi da una cella a tutte le altre, in quattro direzioni; -1 dove non si arriva.
        // La scala non si attraversa.
        private int[,] Distances(Vector2Int from)
        {
            var distances = new int[_settings.Width, _settings.Height];
            for (int y = 0; y < _settings.Height; y++)
            {
                for (int x = 0; x < _settings.Width; x++)
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
                    if (next.x < 0 || next.y < 0 || next.x >= _settings.Width || next.y >= _settings.Height)
                    {
                        continue;
                    }

                    char c = _cells[next.x, next.y];
                    if (c == LevelMap.Rock || c == StairsSymbol || distances[next.x, next.y] >= 0)
                    {
                        continue;
                    }

                    distances[next.x, next.y] = distances[cell.x, cell.y] + 1;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        private static Vector2Int Center(RectInt room)
        {
            return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
        }

        // Un intero tra min e max compresi.
        private int Range(int min, int max)
        {
            return Math.Min(max, min + (int)(_random.NextDouble() * (max - min + 1)));
        }
    }
}

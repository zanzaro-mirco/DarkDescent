using System;
using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Le caverne (D1 della scheda M7): random walk sulla stessa griglia di celle da 4 m della cripta.
    /// Alcuni camminatori partono dal centro e scavano a turno finché il pavimento è la frazione
    /// chiesta. Tendono a tenere la direzione, così scavano cunicoli che si allargano dove si
    /// incrociano; ogni tanto uno riparte da una cella già scavata, e ogni tanto scava un quadrato
    /// di 2 × 2. Poi la smussatura riempie la roccia isolata e toglie le punte, le celle unite solo in
    /// diagonale si collegano, e resta la regione connessa più grande. Ingresso nella zona aperta più
    /// lontana dal centro, scala nel punto più lontano dall'ingresso. Logica pura e solo interi, come
    /// la cripta: stesso seme, stessa caverna.
    /// </summary>
    public sealed class CaveGenerator : ILevelGenerator
    {
        private readonly CaveSettings _settings;

        // lo stato di una generazione: rifatto da capo a ogni Generate
        private IRandomSource _random;
        private char[,] _cells;
        private int _width;
        private int _height;
        private List<Vector2Int> _carved;

        public CaveGenerator(CaveSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public DungeonLayout Generate(ulong seed, int depth = 1)
        {
            _random = new SplitMix64Source(seed);
            _width = _settings.Width;
            _height = _settings.Height;
            _cells = new char[_width, _height];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _cells[x, y] = LevelMap.Rock;
                }
            }

            Walk();
            for (int i = 0; i < _settings.SmoothPasses; i++)
            {
                Smooth();
            }

            JoinDiagonals();
            FillPillars();
            Vector2Int anchor = KeepLargestRegion();

            Vector2Int entrance = FarthestOpenCell(anchor);
            Vector2Int stairs = PlaceStairs(entrance);
            _cells[entrance.x, entrance.y] = DungeonGenerator.EntranceSymbol;
            return new DungeonLayout(_cells, new List<RectInt>(), -1, -1, entrance, stairs);
        }

        private void Walk()
        {
            int target = (int)((_width - 2) * (_height - 2) * _settings.FloorFraction);
            var center = new Vector2Int(_width / 2, _height / 2);
            var walkers = new Vector2Int[_settings.Walkers];
            var headings = new int[walkers.Length];
            for (int i = 0; i < walkers.Length; i++)
            {
                walkers[i] = center;
                headings[i] = i % LevelGrid.Steps.Length;
            }

            _carved = new List<Vector2Int>();
            int floor = Carve(center);

            // un tetto ai passi: con i numeri del gioco si arriva al pavimento chiesto molto prima
            int budget = (_width - 2) * (_height - 2) * 200;
            for (int i = 0; floor < target && budget-- > 0; i = (i + 1) % walkers.Length)
            {
                if (_random.NextDouble() < _settings.JumpChance)
                {
                    walkers[i] = _carved[Index(_carved.Count)];
                }

                if (_random.NextDouble() >= _settings.StraightChance)
                {
                    headings[i] = Index(LevelGrid.Steps.Length);
                }

                // un margine di due celle: il quadrato di 2 × 2 resta dentro il bordo di roccia
                var next = walkers[i] + LevelGrid.Steps[headings[i]];
                if (next.x < 2 || next.y < 2 || next.x > _width - 3 || next.y > _height - 3)
                {
                    headings[i] = Index(LevelGrid.Steps.Length);
                    continue;
                }

                walkers[i] = next;
                floor += Carve(next);
                if (_random.NextDouble() < _settings.WideChance)
                {
                    floor += Carve(next + Vector2Int.right) + Carve(next + Vector2Int.up) + Carve(next + Vector2Int.one);
                }
            }
        }

        private int Carve(Vector2Int cell)
        {
            if (_cells[cell.x, cell.y] != LevelMap.Rock)
            {
                return 0;
            }

            _cells[cell.x, cell.y] = LevelMap.Floor;
            _carved.Add(cell);
            return 1;
        }

        // Una passata dell'automa, letta da una copia: la roccia circondata quasi del tutto diventa pavimento,
        // il pavimento con un vicino solo (una punta) diventa roccia. Togliere una punta non stacca niente.
        private void Smooth()
        {
            var before = (char[,])_cells.Clone();
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    int around = FloorAround(before, x, y);
                    int beside = FloorBeside(before, x, y);
                    if (before[x, y] == LevelMap.Rock)
                    {
                        if (beside == 4 || around >= 6)
                        {
                            _cells[x, y] = LevelMap.Floor;
                        }
                    }
                    else if (beside <= 1 && around <= 2)
                    {
                        _cells[x, y] = LevelMap.Rock;
                    }
                }
            }
        }

        // Due pavimenti che si toccano solo in diagonale non sono collegati per il NavMesh né per
        // l'esplorazione (trappola 2): si scava una delle due celle di roccia tra loro.
        private void JoinDiagonals()
        {
            bool changed = true;
            for (int guard = 0; changed && guard < 10; guard++)
            {
                changed = false;
                for (int y = 1; y < _height - 2; y++)
                {
                    for (int x = 1; x < _width - 2; x++)
                    {
                        bool a = IsFloor(x, y), b = IsFloor(x + 1, y), c = IsFloor(x, y + 1), d = IsFloor(x + 1, y + 1);
                        if (a && d && !b && !c)
                        {
                            _cells[x + 1, y] = LevelMap.Floor;
                            changed = true;
                        }
                        else if (b && c && !a && !d)
                        {
                            _cells[x, y] = LevelMap.Floor;
                            changed = true;
                        }
                    }
                }
            }
        }

        // Un pilastro di roccia di una cella, con il pavimento sui quattro lati, coprirebbe il cavaliere
        // con i muri alti (trappola 3).
        private void FillPillars()
        {
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    if (_cells[x, y] == LevelMap.Rock && FloorBeside(_cells, x, y) == 4)
                    {
                        _cells[x, y] = LevelMap.Floor;
                    }
                }
            }
        }

        // Tiene la regione connessa più grande, in quattro direzioni; restituisce una sua cella.
        private Vector2Int KeepLargestRegion()
        {
            var region = new int[_width, _height];
            var sizes = new List<int> { 0 };
            var firsts = new List<Vector2Int> { default };
            var queue = new Queue<Vector2Int>();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_cells[x, y] == LevelMap.Rock || region[x, y] != 0)
                    {
                        continue;
                    }

                    int id = sizes.Count;
                    int size = 0;
                    region[x, y] = id;
                    queue.Enqueue(new Vector2Int(x, y));
                    while (queue.Count > 0)
                    {
                        var cell = queue.Dequeue();
                        size++;
                        foreach (var step in LevelGrid.Steps)
                        {
                            var next = cell + step;
                            if (IsFloor(next.x, next.y) && region[next.x, next.y] == 0)
                            {
                                region[next.x, next.y] = id;
                                queue.Enqueue(next);
                            }
                        }
                    }

                    sizes.Add(size);
                    firsts.Add(new Vector2Int(x, y));
                }
            }

            int largest = 1;
            for (int i = 2; i < sizes.Count; i++)
            {
                if (sizes[i] > sizes[largest])
                {
                    largest = i;
                }
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (region[x, y] != largest)
                    {
                        _cells[x, y] = LevelMap.Rock;
                    }
                }
            }

            return firsts[largest];
        }

        // La cella più lontana a piedi da quella data tra quelle con il pavimento tutto attorno: il
        // cavaliere compare in uno spazio aperto, non in fondo a un cunicolo.
        private Vector2Int FarthestOpenCell(Vector2Int from)
        {
            int[,] distances = LevelGrid.Distances(_cells, from);
            Vector2Int best = from;
            int bestDistance = -1;
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    if (distances[x, y] > bestDistance && FloorAround(_cells, x, y) == 8)
                    {
                        bestDistance = distances[x, y];
                        best = new Vector2Int(x, y);
                    }
                }
            }

            return best;
        }

        // Come nella cripta: la roccia a nord (lo stendardo), il pavimento a sud (da dove si arriva), e
        // niente che resti tagliato fuori. Tra le celle buone, la più lontana dall'ingresso.
        private Vector2Int PlaceStairs(Vector2Int entrance)
        {
            int[,] distances = LevelGrid.Distances(_cells, entrance);
            var candidates = new List<Vector2Int>();
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 1; x++)
                {
                    if (_cells[x, y] == LevelMap.Floor && _cells[x, y - 1] == LevelMap.Rock && _cells[x, y + 1] == LevelMap.Floor
                        && distances[x, y] > 0)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            // ordine stabile: prima la più lontana, poi la più a nord, poi la più a ovest
            candidates.Sort((a, b) => distances[a.x, a.y] != distances[b.x, b.y] ? distances[b.x, b.y].CompareTo(distances[a.x, a.y])
                : a.y != b.y ? a.y.CompareTo(b.y)
                : a.x.CompareTo(b.x));
            foreach (var cell in candidates)
            {
                _cells[cell.x, cell.y] = DungeonGenerator.StairsSymbol;
                if (LevelGrid.AllFloorReachable(_cells, entrance))
                {
                    return cell;
                }

                _cells[cell.x, cell.y] = LevelMap.Floor;
            }

            throw new InvalidOperationException("Nessun posto per la scala nella caverna.");
        }

        private bool IsFloor(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _width && y < _height && _cells[x, y] != LevelMap.Rock;
        }

        private static int FloorBeside(char[,] cells, int x, int y)
        {
            int count = 0;
            foreach (var step in LevelGrid.Steps)
            {
                if (cells[x + step.x, y + step.y] != LevelMap.Rock)
                {
                    count++;
                }
            }

            return count;
        }

        private static int FloorAround(char[,] cells, int x, int y)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx != 0 || dy != 0) && cells[x + dx, y + dy] != LevelMap.Rock)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        // Un intero tra 0 e count escluso.
        private int Index(int count)
        {
            return Math.Min(count - 1, (int)(_random.NextDouble() * count));
        }
    }
}

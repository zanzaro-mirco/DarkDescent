using System.Collections.Generic;

namespace DarkDescent.Levels
{
    /// <summary>
    /// La mappa scoperta di ogni profondità visitata nella partita (D8 della M8). Tornando a una
    /// profondità, il livello si rigenera dal seme uguale a prima e riprende le celle già viste;
    /// da un salvataggio, quelle salvate. Logica pura.
    /// </summary>
    public sealed class ExplorationMemory
    {
        private readonly Dictionary<int, Exploration> _levels = new Dictionary<int, Exploration>();

        // dal salvataggio, in attesa che il livello di quella profondità venga generato
        private readonly Dictionary<int, ExploredLevel> _saved = new Dictionary<int, ExploredLevel>();

        /// <summary>
        /// L'esplorazione del livello appena generato a questa profondità, con le celle già viste
        /// prima, o salvate. Una mappa di un'altra misura riparte da niente.
        /// </summary>
        public Exploration Open(int depth, LevelMap map)
        {
            var exploration = new Exploration(map);
            if (_levels.TryGetValue(depth, out var previous))
            {
                exploration.CopyExplored(previous);
            }
            else if (_saved.TryGetValue(depth, out var saved))
            {
                if (saved.Width == map.Width && saved.Height == map.Height)
                {
                    exploration.LoadCells(saved.Cells());
                }

                _saved.Remove(depth);
            }

            _levels[depth] = exploration;
            return exploration;
        }

        /// <summary>Le mappe scoperte da salvare: quelle visitate e quelle salvate non ancora riaperte.</summary>
        public List<ExploredLevel> Export()
        {
            var result = new List<ExploredLevel>();
            foreach (var pair in _levels)
            {
                result.Add(new ExploredLevel(pair.Key, pair.Value.Map.Width, pair.Value.Map.Height, pair.Value.CellsToBytes()));
            }

            foreach (var pair in _saved)
            {
                result.Add(pair.Value);
            }

            result.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            return result;
        }

        /// <summary>Dimentica tutto e tiene le mappe di un salvataggio, per quando si riapriranno i livelli.</summary>
        public void Import(IEnumerable<ExploredLevel> levels)
        {
            _levels.Clear();
            _saved.Clear();
            if (levels == null)
            {
                return;
            }

            foreach (var level in levels)
            {
                if (level != null)
                {
                    _saved[level.Depth] = level;
                }
            }
        }
    }
}

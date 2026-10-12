using System.Collections.Generic;

namespace DarkDescent.Levels
{
    /// <summary>
    /// I ricordi dei livelli visitati nella partita (10.9 della M10), uno per scena e profondità: ogni
    /// livello rientrandoci si rigenera dal seme e poi torna com'era. Vanno nel salvataggio dal formato
    /// 5. Logica pura.
    /// </summary>
    public sealed class LevelMemory
    {
        private readonly Dictionary<string, LevelRecord> _records = new Dictionary<string, LevelRecord>();

        public int Count => _records.Count;

        /// <summary>Il ricordo nuovo prende il posto di quello dello stesso livello.</summary>
        public void Remember(LevelRecord record)
        {
            if (record != null)
            {
                _records[Key(record.Scene, record.Depth)] = record;
            }
        }

        public bool TryGet(string scene, int depth, out LevelRecord record)
        {
            return _records.TryGetValue(Key(scene, depth), out record);
        }

        public List<LevelRecord> Export()
        {
            return new List<LevelRecord>(_records.Values);
        }

        /// <summary>Al posto dei ricordi di adesso, quelli di un salvataggio.</summary>
        public void Import(IEnumerable<LevelRecord> records)
        {
            _records.Clear();
            if (records == null)
            {
                return;
            }

            foreach (var record in records)
            {
                Remember(record);
            }
        }

        private static string Key(string scene, int depth)
        {
            return scene + "#" + depth;
        }
    }
}

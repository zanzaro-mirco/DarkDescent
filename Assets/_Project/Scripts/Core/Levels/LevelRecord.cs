using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDescent.Levels
{
    /// <summary>
    /// Quello che è cambiato in un livello da quando il seme l'ha generato (10.9 della M10), come in
    /// Diablo 1: i nemici uccisi e le casse aperte, per posto nell'ordine in cui il livello li
    /// costruisce, e gli oggetti rimasti a terra. Con quanti nemici e casse c'erano si riconosce un
    /// ricordo che non corrisponde più al livello generato: allora nemici e casse ripartono come nuovi.
    /// </summary>
    [Serializable]
    public sealed class LevelRecord
    {
        [SerializeField] private string _scene;
        [SerializeField] private int _depth;
        [SerializeField] private int _enemyCount;
        [SerializeField] private int _chestCount;
        [SerializeField] private List<int> _killed = new List<int>();
        [SerializeField] private List<int> _opened = new List<int>();
        [SerializeField] private List<GroundItemRecord> _items = new List<GroundItemRecord>();

        public LevelRecord(string scene, int depth, int enemyCount, int chestCount)
        {
            _scene = scene;
            _depth = depth;
            _enemyCount = enemyCount;
            _chestCount = chestCount;
        }

        public string Scene => _scene;

        public int Depth => _depth;

        /// <summary>I posti dei nemici uccisi.</summary>
        public IReadOnlyList<int> Killed => _killed ??= new List<int>();

        /// <summary>I posti delle casse aperte.</summary>
        public IReadOnlyList<int> Opened => _opened ??= new List<int>();

        public IReadOnlyList<GroundItemRecord> Items => _items ??= new List<GroundItemRecord>();

        /// <summary>Il livello generato ha gli stessi nemici e le stesse casse del ricordo.</summary>
        public bool Matches(int enemyCount, int chestCount)
        {
            return enemyCount == _enemyCount && chestCount == _chestCount;
        }

        public void AddKilled(int index)
        {
            _killed.Add(index);
        }

        public void AddOpened(int index)
        {
            _opened.Add(index);
        }

        public void AddItem(GroundItemRecord item)
        {
            _items.Add(item);
        }
    }
}

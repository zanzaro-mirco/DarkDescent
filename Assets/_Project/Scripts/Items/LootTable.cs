using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Cosa lascia un nemico (D8 della M5), immutabile: la probabilità di lasciare qualcosa e le
    /// basi pesate. Rarità e affissi li decide poi il generatore.
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "DarkDescent/Items/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        [Tooltip("Probabilità di lasciare un oggetto alla morte, tra 0 e 1.")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.7f;

        [SerializeField] private List<LootEntry> _entries = new List<LootEntry>();

        public float DropChance => _dropChance;

        public IReadOnlyList<LootEntry> Entries => _entries;

        /// <summary>Tira il drop: false se il nemico non lascia niente, altrimenti la base estratta per peso.</summary>
        public bool TryRoll(IRandomSource random, out ItemDefinition item)
        {
            item = null;
            if (random.NextDouble() >= _dropChance)
            {
                return false;
            }

            int total = 0;
            foreach (var entry in _entries)
            {
                if (entry.Item != null)
                {
                    total += entry.Weight;
                }
            }

            if (total <= 0)
            {
                return false;
            }

            double roll = random.NextDouble() * total;
            foreach (var entry in _entries)
            {
                if (entry.Item == null || entry.Weight <= 0)
                {
                    continue;
                }

                item = entry.Item;
                roll -= entry.Weight;
                if (roll < 0.0)
                {
                    break;
                }
            }

            return true;
        }
    }
}

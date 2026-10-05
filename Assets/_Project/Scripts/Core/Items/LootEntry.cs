using System;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>Una riga di una loot table: una base e il suo peso nell'estrazione.</summary>
    [Serializable]
    public struct LootEntry
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(0)] private int _weight;

        public LootEntry(ItemDefinition item, int weight)
        {
            _item = item;
            _weight = weight;
        }

        public ItemDefinition Item => _item;

        public int Weight => _weight;
    }
}

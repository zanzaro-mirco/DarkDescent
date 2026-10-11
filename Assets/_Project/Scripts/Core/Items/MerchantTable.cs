using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Cosa vende il mercante (D5 della M10), immutabile: le provviste, sempre in vendita e senza
    /// fine, e le basi pesate degli oggetti che tira a ogni ritorno in città.
    /// </summary>
    [CreateAssetMenu(fileName = "MerchantTable", menuName = "DarkDescent/Items/Merchant Table")]
    public sealed class MerchantTable : ScriptableObject
    {
        [Tooltip("Sempre in vendita, normali e senza limite: pozioni e pergamene del ritorno.")]
        [SerializeField] private List<ItemDefinition> _supplies = new List<ItemDefinition>();

        [Tooltip("Le basi degli oggetti in vendita, con il loro peso.")]
        [SerializeField] private List<LootEntry> _stock = new List<LootEntry>();

        [Tooltip("Quanti oggetti tira a ogni ritorno in città.")]
        [SerializeField, Min(1)] private int _stockSize = 8;

        public IReadOnlyList<ItemDefinition> Supplies => _supplies;

        public IReadOnlyList<LootEntry> Stock => _stock;

        public int StockSize => _stockSize;

        /// <summary>Una base estratta per peso; null se la tabella non ne ha.</summary>
        public ItemDefinition PickStock(IRandomSource random)
        {
            int total = 0;
            foreach (var entry in _stock)
            {
                if (entry.Item != null)
                {
                    total += entry.Weight;
                }
            }

            if (total <= 0)
            {
                return null;
            }

            double roll = random.NextDouble() * total;
            ItemDefinition picked = null;
            foreach (var entry in _stock)
            {
                if (entry.Item == null || entry.Weight <= 0)
                {
                    continue;
                }

                picked = entry.Item;
                roll -= entry.Weight;
                if (roll < 0.0)
                {
                    break;
                }
            }

            return picked;
        }
    }
}

using System.Collections.Generic;
using DarkDescent.Core;
using UnityEngine;

namespace DarkDescent.Items
{
    /// <summary>
    /// Cosa lascia un nemico (D8 della M5), immutabile: la probabilità di lasciare qualcosa e le
    /// basi pesate. Rarità e affissi li decide poi il generatore. La pozione ha un tiro suo, in più
    /// dell'oggetto (D14 della M6): di cura o, dalla M9, di mana.
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "DarkDescent/Items/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        [Tooltip("Probabilità di lasciare un oggetto alla morte, tra 0 e 1.")]
        [SerializeField, Range(0f, 1f)] private float _dropChance = 0.7f;

        [SerializeField] private List<LootEntry> _entries = new List<LootEntry>();

        [Tooltip("La pozione che può lasciare in più dell'oggetto. Vuoto: niente pozioni.")]
        [SerializeField] private PotionDefinition _potion;

        [Tooltip("Probabilità di lasciare anche la pozione, tra 0 e 1: 0,25 gli scheletri, 0,5 le casse.")]
        [SerializeField, Range(0f, 1f)] private float _potionChance;

        [Tooltip("La pozione di mana, che cade al posto di quella di cura quando quella non cade (D2 della M9). Vuoto: niente.")]
        [SerializeField] private PotionDefinition _manaPotion;

        [Tooltip("Probabilità della pozione di mana, tra 0 e 1, in più di quella di cura: la somma delle due non supera 1.")]
        [SerializeField, Range(0f, 1f)] private float _manaPotionChance;

        public float DropChance => _dropChance;

        public IReadOnlyList<LootEntry> Entries => _entries;

        public PotionDefinition Potion => _potion;

        public float PotionChance => _potionChance;

        public PotionDefinition ManaPotion => _manaPotion;

        public float ManaPotionChance => _manaPotionChance;

        /// <summary>
        /// La pozione di un tiro tra 0 e 1, o null: sotto <see cref="PotionChance"/> quella di cura, nel
        /// tratto dopo quella di mana. Un tiro solo: la pozione di cura cade dove cadeva prima della M9.
        /// </summary>
        public PotionDefinition PotionFor(double roll)
        {
            if (_potion != null && roll < _potionChance)
            {
                return _potion;
            }

            return _manaPotion != null && roll >= _potionChance && roll < _potionChance + _manaPotionChance ? _manaPotion : null;
        }

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

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

        [Tooltip("La rarità più bassa dell'oggetto: raro per il boss (D8 della M10), normale per tutti gli altri.")]
        [SerializeField] private Rarity _minRarity = Rarity.Normal;

        [Tooltip("La pozione che può lasciare in più dell'oggetto. Vuoto: niente pozioni.")]
        [SerializeField] private PotionDefinition _potion;

        [Tooltip("Probabilità di lasciare anche la pozione, tra 0 e 1: 0,25 gli scheletri, 0,5 le casse.")]
        [SerializeField, Range(0f, 1f)] private float _potionChance;

        [Tooltip("La pozione di mana, che cade al posto di quella di cura quando quella non cade (D2 della M9). Vuoto: niente.")]
        [SerializeField] private PotionDefinition _manaPotion;

        [Tooltip("Probabilità della pozione di mana, tra 0 e 1, in più di quella di cura: la somma delle due non supera 1.")]
        [SerializeField, Range(0f, 1f)] private float _manaPotionChance;

        [Tooltip("La pila d'oro che può lasciare (D4 della M10). Vuoto: niente oro.")]
        [SerializeField] private GoldDefinition _gold;

        [Tooltip("Probabilità di lasciare oro, tra 0 e 1.")]
        [SerializeField, Range(0f, 1f)] private float _goldChance;

        [Tooltip("Quanto oro alla profondità 1, da e a.")]
        [SerializeField] private Vector2Int _goldRange = new Vector2Int(1, 1);

        [Tooltip("Quanto cresce l'oro a ogni profondità in più: 0,35 vuol dire +35% a livello.")]
        [SerializeField, Min(0f)] private float _goldPerDepth = 0.35f;

        public float DropChance => _dropChance;

        public Rarity MinRarity => _minRarity;

        public GoldDefinition Gold => _gold;

        public float GoldChance => _goldChance;

        public Vector2Int GoldRange => _goldRange;

        public float GoldPerDepth => _goldPerDepth;

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

        /// <summary>
        /// L'oro di un tiro, 0 se non ne cade: sotto la probabilità una quantità nell'intervallo,
        /// cresciuta con la profondità e arrotondata, almeno 1.
        /// </summary>
        public int GoldFor(IRandomSource random, int depth)
        {
            if (_gold == null || random.NextDouble() >= _goldChance)
            {
                return 0;
            }

            int min = Mathf.Max(1, _goldRange.x);
            int max = Mathf.Max(min, _goldRange.y);
            int amount = Mathf.Min(max, min + (int)(random.NextDouble() * (max - min + 1)));
            double scaled = amount * (1.0 + (Mathf.Max(1, depth) - 1) * _goldPerDepth);
            return System.Math.Max(1, (int)System.Math.Round(scaled, System.MidpointRounding.AwayFromZero));
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

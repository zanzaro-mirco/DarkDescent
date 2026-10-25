using System.Collections.Generic;
using System.Linq;
using DarkDescent.Editor;
using DarkDescent.Items;
using DarkDescent.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// La misura di unici e set (D11 della M16, passo 16.6): quanti ne cadono a discesa per ogni
    /// difficoltà, e quanto rendono rispetto ai rari della stessa base.
    /// </summary>
    public class NamedCensusTests
    {
        private static readonly int[] Levels = { 20, 35, 55 };

        private DifficultyTable _table;
        private NamedItemDatabase _items;
        private List<LootCensus.Tally> _tallies;

        [OneTimeSetUp]
        public void Measure()
        {
            _table = AssetDatabase.LoadAssetAtPath<DifficultyTable>(DifficultyCensus.DifficultiesPath);
            _items = AssetDatabase.LoadAssetAtPath<NamedItemDatabase>(LootCensus.NamedItemsPath);
            _tallies = LootCensus.NamedPerDifficulty(_table, LootCensus.FixedSeeds(20));
            Debug.Log("[NamedCensus] " + CensusChecks.DescribeNamed(_tallies, _table));
        }

        private static LootRoller Roller(ulong seed, int itemLevel)
        {
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes;
            return new LootRoller(new ItemGenerator(affixes, RarityTable.Default), seed, _ => itemLevel);
        }

        [Test, Description("Unici e set a discesa: all'Ombra 1–2, all'ultimo Abisso 3–5; al Crepuscolo pochi, e solo dal tesoro del re")]
        public void Drops_StayInTheBands()
        {
            Assert.AreEqual(_table.Count, _tallies.Count);
            MapChecks.Pass("misura di unici e set", CensusChecks.NamedDrops(_tallies));
        }

        [Test, Description("Più si scende di difficoltà, più unici e set: ogni gradino almeno quanto il precedente")]
        public void Drops_GrowWithTheDifficulty()
        {
            for (int t = 1; t < _tallies.Count; t++)
            {
                double before = _tallies[t - 1].Unique + _tallies[t - 1].Set;
                double now = _tallies[t].Unique + _tallies[t].Set;
                Assert.GreaterOrEqual(now, before * 0.99, $"{_table.Tier(t).NameKey} dopo {_table.Tier(t - 1).NameKey}");
            }
        }

        [Test, Description("Il conto atteso torna con i tiri veri del LootRoller: scheletri a una probabilità alta e il tesoro del re al Crepuscolo")]
        public void Expectation_MatchesTheRoller()
        {
            const int level = 30;
            var skeleton = AssetDatabase.LoadAssetAtPath<LootTable>("Assets/_Project/Data/Loot/SkeletonLoot.asset");
            var roller = Roller(77UL, level);
            roller.UseNamed(_items);
            roller.UseNamedChances(0.1f, 0.1f);
            var (coverUnique, coverSet, _) = LootCensus.Coverage(skeleton, _items, level);
            int unique = 0, set = 0;
            const int rolls = 20000;
            for (int x = 0; x < rolls; x++)
            {
                var item = roller.Roll(skeleton, 3, x, 7);
                unique += item != null && item.Rarity == Rarity.Unique ? 1 : 0;
                set += item != null && item.Rarity == Rarity.Set ? 1 : 0;
            }

            Assert.AreEqual(rolls * skeleton.DropChance * 0.1 * coverUnique, unique, rolls * skeleton.DropChance * 0.1 * coverUnique * 0.15 + 5);
            Assert.AreEqual(rolls * skeleton.DropChance * 0.1 * coverSet, set, rolls * skeleton.DropChance * 0.1 * coverSet * 0.15 + 5);

            // il tesoro del re al Crepuscolo: solo il tiro suo
            var treasure = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Dungeon/KingsTreasure.prefab").GetComponent<Chest>();
            var king = Roller(78UL, 16);
            king.UseNamed(_items);
            var twilight = LootCensus.Count(new[] { new LootCensus.Source("tesoro del re", treasure.LootTable, treasure.Bonus, 1f) }, 8,
                new LootCensus.NamedOdds(_items, 0f, 0f, _ => 16));
            var extra = new List<ItemInstance>();
            int named = 0;
            const int chests = 4000;
            for (int x = 0; x < chests; x++)
            {
                king.RollExtra(treasure.LootTable, treasure.Bonus, null, 8, x, 3, extra);
                named += extra.Count(item => item.IsNamed);
            }

            double expected = (twilight.Unique + twilight.Set) * chests;
            Assert.Greater(expected, 0.0);
            Assert.AreEqual(expected, named, expected * 0.15 + 5);
        }

        [Test, Description("Ogni unico rende almeno il 30% più di un raro della stessa base, ai livelli 20, 35 e 55, contro il bruto della difficoltà di quel livello")]
        public void Uniques_BeatRares()
        {
            var census = new DifficultyCensus(1, 64);
            var rows = new List<DifficultyCensus.NamedPower>();
            foreach (var unique in _items.Items.Where(u => u.Set == null))
            {
                rows.AddRange(Levels.Where(l => l >= unique.MinLevel).Select(l => census.CompareNamed(unique, l, _table)));
            }

            Debug.Log("[NamedCensus] unici contro rari\n" + CensusChecks.DescribePower(rows));
            MapChecks.Pass("unici contro rari", CensusChecks.NamedPower(rows));
        }

        [Test, Description("Ogni set intero, con i suoi bonus, rende almeno il 30% più di quattro rari delle stesse basi")]
        public void FullSets_BeatRares()
        {
            var census = new DifficultyCensus(1, 64);
            var sets = _items.Items.Where(u => u.Set != null).Select(u => u.Set).Distinct().ToList();
            Assert.AreEqual(2, sets.Count);
            var rows = sets.SelectMany(s => Levels.Select(l => census.CompareSet(s, l, _table))).ToList();

            Debug.Log("[NamedCensus] set contro rari\n" + CensusChecks.DescribePower(rows));
            MapChecks.Pass("set contro rari", CensusChecks.NamedPower(rows));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Editor;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il loot misurato (D13 della M13): meno oggetti, ma migliori.</summary>
    public class LootCensusTests
    {
        private static LootTable Table(string name) => AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");

        private static ChampionSettings Champions => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private static LootRoller Roller(ulong seed)
        {
            var affixes = AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset").Affixes;
            return new LootRoller(new ItemGenerator(affixes, RarityTable.Default), seed);
        }

        [Test, Description("In media da 4 a 5 oggetti a livello dalla profondità 1 alla 8, misurati sui livelli generati come nel gioco")]
        public void AverageLoot_StaysInTheBand()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(LootCensus.CryptSettings);
            var caves = AssetDatabase.LoadAssetAtPath<DungeonSettings>(LootCensus.CaveSettings);
            var total = LootCensus.Range(crypt, AssetDatabase.LoadAssetAtPath<LevelTileset>(LootCensus.CryptTileset), Champions, 1, 4, 40);
            total.Merge(LootCensus.Range(caves, AssetDatabase.LoadAssetAtPath<LevelTileset>(LootCensus.CaveTileset), Champions, 5, 8, 40));
            Assert.AreEqual(320, total.Levels);
            MapChecks.Pass("misura del loot", CensusChecks.Loot(total));
        }

        [Test, Description("I numeri della D13: scheletro 15%, mago 25%, bruto 35%, sciame 4%, cassa 35%; forziere accanto al 25% dei campioni")]
        public void Tables_HaveTheNewChances()
        {
            Assert.AreEqual(0.15f, Table("SkeletonLoot").DropChance, 1e-5f);
            Assert.AreEqual(0.25f, Table("MageLoot").DropChance, 1e-5f);
            Assert.AreEqual(0.35f, Table("BruteLoot").DropChance, 1e-5f);
            Assert.AreEqual(0.04f, Table("SwarmLoot").DropChance, 1e-5f);
            Assert.AreEqual(0.35f, Table("ChestLoot").DropChance, 1e-5f);
            foreach (var path in new[] { LootCensus.CryptSettings, LootCensus.CaveSettings })
            {
                Assert.AreEqual(0.25f, AssetDatabase.LoadAssetAtPath<DungeonSettings>(path).Specials.GoldChestChance, 1e-5f, path);
            }
        }

        [Test, Description("Campione: un oggetto al 50% con le rarità spostate, al posto del tiro di sempre; unico: due oggetti, uno raro; forziere: un oggetto sicuro, almeno magico")]
        public void Bonuses_ReplaceTheRoll()
        {
            var champion = Champions.For(EnemyRank.Champion).Loot;
            Assert.IsTrue(champion.ReplacesRoll);
            Assert.AreEqual(1, champion.ExtraRolls);
            Assert.AreEqual(0.5f, champion.ExtraChance, 1e-5f);
            Assert.Greater(champion.RareFactor, 1f);

            var unique = Champions.For(EnemyRank.Unique).Loot;
            Assert.IsTrue(unique.ReplacesRoll);
            Assert.AreEqual(2, unique.ExtraRolls);
            Assert.AreEqual(1f, unique.ExtraChance);
            Assert.AreEqual(Rarity.Rare, unique.Guaranteed);

            var goldChest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Dungeon/GoldChest.prefab").GetComponent<Chest>().Bonus;
            Assert.IsTrue(goldChest.ReplacesRoll);
            Assert.AreEqual(1, goldChest.ExtraRolls);
            Assert.AreEqual(Rarity.Magic, goldChest.Guaranteed);
        }

        [Test, Description("Con metà della probabilità la scorta lascia un oggetto la metà delle volte, e quando lo lascia è lo stesso del nemico normale")]
        public void EscortRoll_HalvesTheChance()
        {
            var roller = Roller(4711UL);
            var table = Table("BruteLoot");
            int full = 0;
            int half = 0;
            for (int x = 0; x < 3000; x++)
            {
                var normal = roller.Roll(table, 3, x, 11);
                var escort = roller.Roll(table, 3, x, 11, 0.5f);
                full += normal != null ? 1 : 0;
                half += escort != null ? 1 : 0;
                if (escort != null)
                {
                    Assert.IsNotNull(normal);
                    Assert.AreEqual(normal.Definition, escort.Definition);
                    Assert.AreEqual(normal.Seed, escort.Seed);
                }
            }

            Assert.That((double)half / full, Is.InRange(0.43, 0.57));
        }

        [Test, Description("Il tiro in più del campione cade una volta su due")]
        public void ChampionExtra_FallsHalfTheTime()
        {
            var roller = Roller(99UL);
            var bonus = Champions.For(EnemyRank.Champion).Loot;
            var items = new List<ItemInstance>();
            int dropped = 0;
            for (int x = 0; x < 2000; x++)
            {
                roller.RollExtra(Table("SkeletonLoot"), bonus, null, 4, x, -3, items);
                Assert.LessOrEqual(items.Count, 1);
                dropped += items.Count;
            }

            Assert.That(dropped, Is.InRange(900, 1100));
        }

        [Test, Description("Le scorte sono scritte nella mappa: nemici normali, mai i campioni, dalla profondità dei nemici speciali")]
        public void Escorts_AreInTheMap()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>(LootCensus.CryptSettings);
            int escorts = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var map = DungeonLevel.CreateMap(crypt, seed, 3, out var layout);
                var cells = SpecialEnemyPlanner.EscortsFromMap(map);
                var champions = new HashSet<Vector2Int>(ChampionSpawn.FromMap(map).Select(c => new Vector2Int(c.X, c.Y)));
                foreach (var cell in cells)
                {
                    Assert.IsTrue(SpecialEnemyPlanner.CanBeChampion(layout[cell.x, cell.y]), $"seme {seed}: ({cell.x}, {cell.y}) non è un nemico");
                    Assert.IsFalse(champions.Contains(cell), $"seme {seed}: la scorta è un campione");
                }

                escorts += cells.Count;
                Assert.AreEqual(0, SpecialEnemyPlanner.EscortsFromMap(DungeonLevel.CreateMap(crypt, seed, 1, out _)).Count, "niente scorte alla profondità 1");
            }

            Assert.Greater(escorts, 30);
        }
    }
}

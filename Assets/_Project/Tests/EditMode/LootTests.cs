using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Items;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class LootTests
    {
        private static LootTable Skeleton => AssetDatabase.LoadAssetAtPath<LootTable>("Assets/_Project/Data/Loot/SkeletonLoot.asset");
        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");

        private static LootRoller Roller(ulong seed) => new LootRoller(new ItemGenerator(Affixes.Affixes, RarityTable.Default), seed);

        private static string Describe(ItemInstance item)
        {
            return item == null
                ? "niente"
                : $"{item.Definition.name} {item.Rarity} L{item.ItemLevel} [{string.Join(", ", item.Affixes.Select(a => $"{a.AffixId} {a.Value}"))}]";
        }

        [Test, Description("Il seme di un nemico è stabile e cambia con seme della partita, profondità e cella")]
        public void SeedMixer_IsStableAndSpreads()
        {
            ulong seed = SeedMixer.ForEnemy(4711UL, 1, 120, -40);

            Assert.AreEqual(seed, SeedMixer.ForEnemy(4711UL, 1, 120, -40));
            var others = new[]
            {
                SeedMixer.ForEnemy(4712UL, 1, 120, -40),
                SeedMixer.ForEnemy(4711UL, 2, 120, -40),
                SeedMixer.ForEnemy(4711UL, 1, 121, -40),
                SeedMixer.ForEnemy(4711UL, 1, 120, -39),
                SeedMixer.ForEnemy(4711UL, 1, -40, 120),
            };
            CollectionAssert.DoesNotContain(others, seed);
            Assert.AreEqual(others.Length, others.Distinct().Count());
        }

        [Test, Description("Stesso seme della partita: gli stessi nemici lasciano gli stessi oggetti, anche chiesti in ordine inverso")]
        public void SameRunSeed_SameDrops_InAnyOrder()
        {
            var cells = Enumerable.Range(0, 20).Select(i => (x: i * 40, z: 100 - i * 13)).ToArray();
            var forward = Roller(4711UL);
            var backward = Roller(4711UL);

            var first = cells.Select(c => Describe(forward.Roll(Skeleton, 1, c.x, c.z))).ToList();
            var second = cells.Reverse().Select(c => Describe(backward.Roll(Skeleton, 1, c.x, c.z))).Reverse().ToList();

            CollectionAssert.AreEqual(first, second);
            var other = cells.Select(c => Describe(Roller(4712UL).Roll(Skeleton, 1, c.x, c.z))).ToList();
            CollectionAssert.AreNotEqual(first, other, "un seme diverso dà altri drop");
        }

        [Test, Description("Su 10.000 nemici lascia qualcosa circa il 70%; il livello dell'oggetto è la profondità; la lama è la base più frequente")]
        public void Drops_FollowTheTable()
        {
            var roller = Roller(99UL);
            int drops = 0;
            var bases = new Dictionary<string, int>();
            for (int i = 0; i < 10000; i++)
            {
                var item = roller.Roll(Skeleton, 2, i, -i);
                if (item == null)
                {
                    continue;
                }

                drops++;
                Assert.AreEqual(2, item.ItemLevel);
                bases[item.Definition.name] = bases.TryGetValue(item.Definition.name, out int n) ? n + 1 : 1;
            }

            Assert.AreEqual(0.7, drops / 10000.0, 0.02);
            Assert.AreEqual(8, bases.Count, "ogni base deve poter cadere");
            Assert.AreEqual("SkeletonBlade", bases.OrderByDescending(p => p.Value).First().Key);
        }

        [Test, Description("La loot table: sotto la probabilità di drop cade la base estratta per peso, sopra niente")]
        public void LootTable_RollsDropThenBase()
        {
            Assert.IsTrue(Skeleton.TryRoll(new FixedRandomSource(0.69, 0.0), out var first));
            Assert.AreEqual("SkeletonBlade", first.name, "la prima della lista");
            Assert.IsFalse(Skeleton.TryRoll(new FixedRandomSource(0.7), out _));
            Assert.IsTrue(Skeleton.TryRoll(new FixedRandomSource(0.0, 0.999), out var last));
            Assert.AreEqual("SpikedShield", last.name, "l'ultima della lista");
        }

        [Test, Description("Su 10.000 nemici delle caverne (D11): lo sciame lascia un oggetto il 20% delle volte e una pozione il 10%, il bruto sempre un oggetto e una pozione la metà delle volte; il livello dell'oggetto è la profondità")]
        public void CaveEnemies_DropAsD11()
        {
            var roller = Roller(4711UL);
            foreach (var (name, items, potions) in new[] { ("SwarmLoot", 0.2, 0.1), ("BruteLoot", 1.0, 0.5) })
            {
                var table = AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");
                int dropped = 0, drunk = 0;
                for (int i = 0; i < 10000; i++)
                {
                    var item = roller.Roll(table, 6, i, -3 * i);
                    if (item != null)
                    {
                        dropped++;
                        Assert.AreEqual(6, item.ItemLevel);
                    }

                    drunk += roller.RollPotion(table, 6, i, -3 * i) != null ? 1 : 0;
                }

                Assert.AreEqual(items, dropped / 10000.0, 0.015, $"{name}: oggetti");
                Assert.AreEqual(potions, drunk / 10000.0, 0.015, $"{name}: pozioni");
            }
        }
    }
}

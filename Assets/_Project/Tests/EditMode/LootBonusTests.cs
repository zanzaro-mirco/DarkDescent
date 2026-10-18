using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class LootBonusTests
    {
        private static LootTable Skeleton => AssetDatabase.LoadAssetAtPath<LootTable>("Assets/_Project/Data/Loot/SkeletonLoot.asset");
        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
        private static ChampionSettings Champions => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        private static LootRoller Roller(ulong seed) => new LootRoller(new ItemGenerator(Affixes.Affixes, RarityTable.Default), seed);

        private static string Describe(ItemInstance item)
        {
            return item == null ? "niente" : $"{item.Definition.name} {item.Rarity} {item.Seed}";
        }

        [Test, Description("Le rarità spostate dei campioni: magico ×2 e raro ×3 danno i pesi 65, 56 e 21")]
        public void Shifted_MultipliesMagicAndRare()
        {
            var shifted = RarityTable.Default.Shifted(2f, 3f);
            Assert.AreEqual(65.0 / 142.0, shifted.Probability(Rarity.Normal), 1e-9);
            Assert.AreEqual(56.0 / 142.0, shifted.Probability(Rarity.Magic), 1e-9);
            Assert.AreEqual(21.0 / 142.0, shifted.Probability(Rarity.Rare), 1e-9);
            Assert.AreEqual(0.07, RarityTable.Default.Probability(Rarity.Rare), 1e-9, "quella di partenza non cambia");
        }

        [Test, Description("I numeri della scheda: il campione tira una volta in più con oro ×2,5, l'unico due volte con un raro garantito e oro ×4")]
        public void ChampionSettings_HaveTheLootOfD7()
        {
            var champion = Champions.For(EnemyRank.Champion).Loot;
            Assert.AreEqual(1, champion.ExtraRolls);
            Assert.AreEqual(2f, champion.MagicFactor);
            Assert.AreEqual(3f, champion.RareFactor);
            Assert.AreEqual(Rarity.Normal, champion.Guaranteed);
            Assert.AreEqual(2.5f, champion.GoldFactor);

            var unique = Champions.For(EnemyRank.Unique).Loot;
            Assert.AreEqual(2, unique.ExtraRolls);
            Assert.AreEqual(Rarity.Rare, unique.Guaranteed);
            Assert.AreEqual(4f, unique.GoldFactor);
        }

        [Test, Description("Senza bonus non cade niente in più; con il bonus tanti oggetti quanti i tiri, sempre gli stessi per la stessa cella, e il primo tiro resta quello del nemico normale")]
        public void Extra_IsStableAndLeavesTheFirstRoll()
        {
            var roller = Roller(4711UL);
            var bonus = new LootBonus(2, 2f, 3f, Rarity.Normal, 1f);
            var into = new List<ItemInstance>();

            roller.RollExtra(Skeleton, null, null, 3, 120, -40, into);
            Assert.IsEmpty(into);
            roller.RollExtra(Skeleton, new LootBonus(), null, 3, 120, -40, into);
            Assert.IsEmpty(into, "un bonus vuoto non aggiunge niente");

            var first = Describe(roller.Roll(Skeleton, 3, 120, -40));
            roller.RollExtra(Skeleton, bonus, roller.Roll(Skeleton, 3, 120, -40), 3, 120, -40, into);
            Assert.AreEqual(2, into.Count, "i tiri in più cadono sempre");
            var extra = into.Select(Describe).ToList();
            Assert.AreEqual(first, Describe(roller.Roll(Skeleton, 3, 120, -40)), "il primo oggetto non cambia");

            Roller(4711UL).RollExtra(Skeleton, bonus, null, 3, 120, -40, into);
            CollectionAssert.AreEqual(extra, into.Select(Describe).ToList(), "stessa cella, stessi oggetti");
            Assert.AreNotEqual(extra[0], extra[1], "ogni tiro ha un seme suo");
            Assert.IsTrue(into.All(i => i.ItemLevel == 3), "il livello è la profondità");
        }

        [Test, Description("Su 4000 campioni i tiri in più sono magici o rari più spesso: 77 su 142 contro 35 su 100 di un nemico normale")]
        public void Extra_ShiftsTheRarities()
        {
            var roller = Roller(99UL);
            var bonus = new LootBonus(1, 2f, 3f, Rarity.Normal, 1f);
            var into = new List<ItemInstance>();
            int eligible = 0, better = 0, baseEligible = 0, baseBetter = 0;
            for (int i = 0; i < 4000; i++)
            {
                roller.RollExtra(Skeleton, bonus, null, 2, i, -i, into);
                Assert.AreEqual(1, into.Count);

                // anelli e amuleti sono sempre almeno magici: si contano solo le altre basi
                if (into[0].Definition.MinRarity == Rarity.Normal)
                {
                    eligible++;
                    better += into[0].Rarity > Rarity.Normal ? 1 : 0;
                }

                var normal = roller.Roll(Skeleton, 2, i, -i);
                if (normal != null && normal.Definition.MinRarity == Rarity.Normal)
                {
                    baseEligible++;
                    baseBetter += normal.Rarity > Rarity.Normal ? 1 : 0;
                }
            }

            Assert.AreEqual(0.35, baseBetter / (double)baseEligible, 0.03, "il nemico normale: 28 + 7 su 100");
            Assert.AreEqual(77.0 / 142.0, better / (double)eligible, 0.03, "il campione: 56 + 21 su 142");
        }

        [Test, Description("La rarità garantita: tra il primo oggetto e i tiri in più c'è sempre almeno un raro; se c'era già, l'ultimo tiro resta com'era")]
        public void Extra_GuaranteesTheRarity()
        {
            var roller = Roller(7UL);
            var bonus = new LootBonus(2, 2f, 3f, Rarity.Rare, 1f);
            var plain = new LootBonus(2, 2f, 3f, Rarity.Normal, 1f);
            var into = new List<ItemInstance>();
            var free = new List<ItemInstance>();
            int raised = 0;
            for (int i = 0; i < 500; i++)
            {
                var first = roller.Roll(Skeleton, 4, i * 7, i);
                roller.RollExtra(Skeleton, bonus, first, 4, i * 7, i, into);
                roller.RollExtra(Skeleton, plain, first, 4, i * 7, i, free);
                bool any = (first != null && first.Rarity == Rarity.Rare) || into.Any(item => item.Rarity == Rarity.Rare);
                Assert.IsTrue(any, $"cella {i}: nessun raro");
                Assert.AreEqual(Describe(free[0]), Describe(into[0]), "solo l'ultimo tiro può cambiare");
                bool hadOne = (first != null && first.Rarity == Rarity.Rare) || free.Any(item => item.Rarity == Rarity.Rare);
                if (hadOne)
                {
                    Assert.AreEqual(Describe(free[1]), Describe(into[1]), "c'era già un raro: niente da alzare");
                }
                else
                {
                    raised++;
                    Assert.AreEqual(free[1].Definition, into[1].Definition, "alzato, resta la stessa base");
                }
            }

            Assert.Greater(raised, 0, "qualche volta il raro va alzato");
        }

        [Test, Description("L'oro con il bonus è lo stesso tiro moltiplicato e arrotondato; quando non cade, non cade nemmeno col bonus")]
        public void Gold_IsMultiplied()
        {
            var roller = Roller(4711UL);
            var bonus = new LootBonus(1, 1f, 1f, Rarity.Normal, 2.5f);
            int checkedGold = 0, none = 0;
            for (int i = 0; i < 200; i++)
            {
                int gold = roller.RollGold(Skeleton, 3, i, 2 * i);
                int more = roller.RollGold(Skeleton, 3, i, 2 * i, bonus);
                if (gold == 0)
                {
                    none++;
                    Assert.AreEqual(0, more);
                    continue;
                }

                checkedGold++;
                Assert.AreEqual((int)System.Math.Round(gold * 2.5, System.MidpointRounding.AwayFromZero), more);
                Assert.AreEqual(gold, roller.RollGold(Skeleton, 3, i, 2 * i, null), "senza bonus è quello");
            }

            Assert.Greater(checkedGold, 0);
            Assert.Greater(none, 0, "gli scheletri non lasciano sempre oro");
        }

        [Test, Description("I semi dei tiri in più non coincidono con quelli di oggetto, pozione e oro, né tra loro")]
        public void ExtraSeeds_AreTheirOwn()
        {
            var seeds = new[]
            {
                SeedMixer.ForEnemy(4711UL, 3, 10, 20),
                SeedMixer.ForPotion(4711UL, 3, 10, 20),
                SeedMixer.ForGold(4711UL, 3, 10, 20),
                SeedMixer.ForExtraLoot(4711UL, 3, 10, 20, 0),
                SeedMixer.ForExtraLoot(4711UL, 3, 10, 20, 1),
                SeedMixer.ForExtraLoot(4711UL, 3, 10, 21, 0),
            };
            Assert.AreEqual(seeds.Length, seeds.Distinct().Count());
            Assert.AreEqual(seeds[3], SeedMixer.ForExtraLoot(4711UL, 3, 10, 20, 0));
        }
    }
}

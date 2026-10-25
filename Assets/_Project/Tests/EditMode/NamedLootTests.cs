using System;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Unici e set in gioco (passo 16.3): formule del combattimento, tiro del loot e dati veri.</summary>
    public class NamedLootTests
    {
        private const string NamedFolder = "Assets/_Project/Data/Named";

        private static NamedItemDatabase Named => AssetDatabase.LoadAssetAtPath<NamedItemDatabase>("Assets/_Project/Data/NamedItemDatabase.asset");
        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");
        private static LootTable Table(string name) => AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");
        private static DifficultyTable Difficulties => AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/_Project/Data/Progression/Difficulties.asset");

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static List<UniqueDefinition> AllNamed()
        {
            return AssetDatabase.FindAssets("t:UniqueDefinition", new[] { NamedFolder })
                .Select(guid => Load<UniqueDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        private static List<SetDefinition> AllSets()
        {
            return AssetDatabase.FindAssets("t:SetDefinition", new[] { NamedFolder })
                .Select(guid => Load<SetDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        // il loot di una difficoltà, con o senza gli unici
        private static LootRoller Roller(int difficulty, bool named, ulong seed = 1UL)
        {
            var tier = Difficulties.Tier(difficulty);
            var roller = new LootRoller(new ItemGenerator(Affixes.Affixes, RarityTable.Default), seed, depth => depth + 10 * difficulty + 2);
            roller.UseDifficulty(tier.Rarities, tier.Gold);
            roller.UseNamedChances(tier.UniqueChance, tier.SetChance);
            if (named)
            {
                roller.UseNamed(Named);
            }

            return roller;
        }

        private static string Describe(ItemInstance item)
        {
            return item == null ? "niente" : $"{item.Definition.name} {item.Rarity} L{item.ItemLevel} {item.Seed}";
        }

        [Test, Description("La velocità d'attacco si ferma al +50%, il rubavita al 10%; il danno ridotto lascia almeno 1 e un colpo a zero resta a zero")]
        public void CombatFormulas_ForTheNewProperties()
        {
            Assert.AreEqual(1f, CombatFormulas.AttackSpeedMultiplier(0f));
            Assert.AreEqual(1.2f, CombatFormulas.AttackSpeedMultiplier(20f), 1e-5f);
            Assert.AreEqual(1.5f, CombatFormulas.AttackSpeedMultiplier(80f), 1e-5f, "fermo al 50%");
            Assert.AreEqual(1f, CombatFormulas.AttackSpeedMultiplier(-10f));

            Assert.AreEqual(3f, CombatFormulas.LifeStolen(50f, 6f), 1e-5f);
            Assert.AreEqual(5f, CombatFormulas.LifeStolen(50f, 25f), 1e-5f, "fermo al 10%");
            Assert.AreEqual(0f, CombatFormulas.LifeStolen(-5f, 6f));

            Assert.AreEqual(7f, CombatFormulas.ReduceDamage(10f, 3f));
            Assert.AreEqual(1f, CombatFormulas.ReduceDamage(10f, 30f), "mai sotto 1");
            Assert.AreEqual(0f, CombatFormulas.ReduceDamage(0f, 5f));
            Assert.AreEqual(10f, CombatFormulas.ReduceDamage(10f, 0f));
        }

        [Test, Description("Il tiro a parte non sposta il loot di sempre: al Crepuscolo è identico, all'Ombra cambia solo chi diventa unico o set, con la stessa base e lo stesso livello")]
        public void NamedRoll_KeepsTheLootOfAlwaysUnchanged()
        {
            var table = Table("SkeletonLoot");
            foreach (int difficulty in new[] { 0, 1 })
            {
                var plain = Roller(difficulty, named: false);
                var named = Roller(difficulty, named: true);
                int promoted = 0;
                for (int cell = 0; cell < 4000; cell++)
                {
                    var a = plain.Roll(table, 3, cell, 7);
                    var b = named.Roll(table, 3, cell, 7);
                    Assert.AreEqual(a == null, b == null, $"cella {cell}");
                    if (a == null)
                    {
                        continue;
                    }

                    Assert.AreSame(a.Definition, b.Definition, $"cella {cell}");
                    Assert.AreEqual(a.ItemLevel, b.ItemLevel);
                    if (b.IsNamed)
                    {
                        promoted++;
                        Assert.AreEqual(difficulty, 1, "al Crepuscolo niente unici");
                        Assert.AreEqual(b.Named.Rarity, b.Rarity);
                    }
                    else
                    {
                        Assert.AreEqual(Describe(a), Describe(b), $"cella {cell}");
                        CollectionAssert.AreEqual(a.Affixes.Select(x => x.AffixId + x.Value), b.Affixes.Select(x => x.AffixId + x.Value));
                    }
                }

                if (difficulty == 1)
                {
                    Assert.Greater(promoted, 0, "all'Ombra qualcuno diventa unico o set");
                }
            }
        }

        [Test, Description("Con le probabilità altissime ogni oggetto di una base che ha un unico lo diventa, sempre lo stesso a ogni tiro")]
        public void NamedRoll_PicksFromTheBaseAndIsReproducible()
        {
            var roller = Roller(1, named: true);
            roller.NamedChanceFactor = 1000f;
            var candidates = new List<UniqueDefinition>();
            int named = 0;
            for (int cell = 0; cell < 2000; cell++)
            {
                var item = roller.Roll(Table("BruteLoot"), 5, cell, 3);
                if (item == null)
                {
                    continue;
                }

                // con le probabilità sopra 1 il tiro cade sempre sull'unico: una base con il solo pezzo di
                // set (gli stivali di cuoio) resta com'è, come vuole la D7
                Named.Candidates(item.Definition, item.ItemLevel, Rarity.Unique, candidates);
                Assert.AreEqual(candidates.Count > 0, item.IsNamed, Describe(item));
                if (item.IsNamed)
                {
                    named++;
                    Assert.AreSame(item.Named.Base, item.Definition);
                    CollectionAssert.AreEqual(item.NamedValues, roller.Roll(Table("BruteLoot"), 5, cell, 3).NamedValues);
                }
            }

            Assert.Greater(named, 100);
        }

        [Test, Description("Il tesoro del re al Crepuscolo lascia un unico o un pezzo di set circa una volta su quattro; un forziere no")]
        public void KingsTreasure_SometimesGivesANamedItemAtDusk()
        {
            var treasure = Load<GameObject>("Assets/_Project/Prefabs/Dungeon/KingsTreasure.prefab").GetComponent<Chest>();
            var goldChest = Load<GameObject>("Assets/_Project/Prefabs/Dungeon/GoldChest.prefab").GetComponent<Chest>();
            Assert.AreEqual(10f, treasure.Bonus.NamedFactor);
            Assert.AreEqual(0.25f, treasure.Bonus.NamedGuarantee);
            Assert.AreEqual(3f, goldChest.Bonus.NamedFactor);
            Assert.AreEqual(0f, goldChest.Bonus.NamedGuarantee);

            var roller = Roller(0, named: true);
            var table = Table("TreasureLoot");
            var into = new List<ItemInstance>();
            int withNamed = 0;
            const int Treasures = 2000;
            for (int cell = 0; cell < Treasures; cell++)
            {
                roller.RollExtra(table, treasure.Bonus, null, 8, cell, 11, into);
                int count = into.Count(item => item.IsNamed);
                Assert.LessOrEqual(count, 1, "al Crepuscolo al più uno, quello del tiro suo");
                withNamed += count;

                roller.RollExtra(table, goldChest.Bonus, null, 8, cell, 11, into);
                Assert.IsFalse(into.Any(item => item.IsNamed), "il forziere al Crepuscolo non ne ha");
            }

            Assert.That(withNamed / (double)Treasures, Is.InRange(0.18, 0.3), $"{withNamed} su {Treasures}");
        }

        [Test, Description("Campioni e unici moltiplicano per 3 le probabilità di unici e set")]
        public void Specials_TripleTheNamedChance()
        {
            var settings = Load<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");
            Assert.AreEqual(3f, settings.For(EnemyRank.Champion).Loot.NamedFactor);
            Assert.AreEqual(3f, settings.For(EnemyRank.Unique).Loot.NamedFactor);
            Assert.AreEqual(0f, settings.For(EnemyRank.Champion).Loot.NamedGuarantee);
        }

        [Test, Description("Le probabilità di unici e set per difficoltà: zero al Crepuscolo, 1% all'Ombra, poi +0,4% a gradino dagli Abissi fino al 4% all'Abisso VI (D7, tarata al 16.6)")]
        public void Difficulties_HaveTheNamedChances()
        {
            float[] expected = { 0f, 0.01f, 0.015f, 0.02f, 0.024f, 0.028f, 0.032f, 0.036f, 0.04f };
            Assert.AreEqual(expected.Length, Difficulties.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], Difficulties.Tier(i).UniqueChance, 1e-6f, $"unici, gradino {i}");
                Assert.AreEqual(expected[i], Difficulties.Tier(i).SetChance, 1e-6f, $"set, gradino {i}");
            }
        }

        [Test, Description("Il database ha esattamente gli unici e i pezzi della cartella, con id unici e i nomi nelle due lingue")]
        public void NamedDatabase_MatchesTheFolder()
        {
            var all = AllNamed();
            Assert.AreEqual(20, all.Count, "12 unici e 8 pezzi");
            CollectionAssert.AreEquivalent(all, Named.Items);
            Assert.AreEqual(12, all.Count(n => n.Rarity == Rarity.Unique));
            Assert.AreEqual(8, all.Count(n => n.Rarity == Rarity.Set));
            var sets = AllSets();
            Assert.AreEqual(2, sets.Count);

            var ids = all.Select(n => n.Id).Concat(sets.Select(s => s.Id)).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.AllItemsAreNotNull(ids);

            var csv = Load<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            var localizer = new Localizer(StringTable.Parse(csv.text));
            foreach (string language in new[] { "en", "it" })
            {
                localizer.SetLanguage(language);
                foreach (string key in all.Select(n => n.NameKey).Concat(sets.Select(s => s.NameKey)))
                {
                    StringAssert.DoesNotStartWith("#", localizer.Get(key), $"{key} in {language}");
                }
            }

            foreach (var named in all)
            {
                Assert.IsNotNull(named.Base, named.name);
                Assert.AreNotEqual(EquipSlot.None, named.Base.Slot, named.name);
                Assert.AreEqual(10, named.MinLevel, named.name);
                Assert.IsNotEmpty(named.Properties, named.name);
                foreach (var property in named.Properties)
                {
                    Assert.Greater(property.High, property.Low, $"{named.name} {property.Effect}");
                    Assert.Greater(property.Low, 0, $"{named.name} {property.Effect}");
                }
            }
        }

        [Test, Description("Ogni set ha quattro pezzi che lo riferiscono, su quattro slot diversi, e i bonus a 2, 3 e 4 pezzi")]
        public void Sets_AreConsistent()
        {
            foreach (var set in AllSets())
            {
                Assert.AreEqual(4, set.Pieces.Count, set.name);
                foreach (var piece in set.Pieces)
                {
                    Assert.AreSame(set, piece.Set, $"{set.name}: {piece.name}");
                }

                CollectionAssert.AllItemsAreUnique(set.Pieces.Select(p => p.Base.Slot), set.name);
                CollectionAssert.AreEqual(new[] { 2, 3, 4 }, set.Bonuses.Select(b => b.Pieces), set.name);
                Assert.AreEqual(10, set.MinLevel);
            }
        }

        [Test, Description("Gli unici sono quelli della tabella del 16.1 tarata al 16.6: la Mannaia del Re com'era, l'Ultima Candela con la Vitalità al posto del mana, proprietà per proprietà")]
        public void Uniques_MatchTheApprovedTable()
        {
            var cleaver = Load<UniqueDefinition>($"{NamedFolder}/Uniques/KingsCleaver.asset");
            Assert.AreEqual("Axe", cleaver.Base.name);
            CollectionAssert.AreEqual(
                new[] { (AffixEffect.WeaponDamagePercent, 70, 200), (AffixEffect.Strength, 10, 40), (AffixEffect.LifeSteal, 3, 6), (AffixEffect.Life, 15, 80) },
                cleaver.Properties.Select(p => (p.Effect, p.Low, p.High)));

            var candle = Load<UniqueDefinition>($"{NamedFolder}/Uniques/LastCandle.asset");
            Assert.AreEqual("Amulet", candle.Base.name);
            CollectionAssert.AreEqual(
                new[] { (AffixEffect.ExperienceBonus, 5, 15), (AffixEffect.Magic, 8, 35), (AffixEffect.Vitality, 15, 85), (AffixEffect.Life, 40, 230) },
                candle.Properties.Select(p => (p.Effect, p.Low, p.High)));

            var warden = Load<SetDefinition>($"{NamedFolder}/Sets/Warden.asset");
            var full = warden.Bonuses.Single(b => b.Pieces == 4);
            CollectionAssert.AreEqual(new[] { (AffixEffect.DamageReduction, 4, 26), (AffixEffect.AllAttributes, 5, 27) },
                full.Properties.Select(p => (p.Effect, p.Low, p.High)));
        }
    }
}

using System.Text;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class PotionTests
    {
        private static PotionDefinition Healing => AssetDatabase.LoadAssetAtPath<PotionDefinition>("Assets/_Project/Data/Items/HealingPotion.asset");
        private static LootTable Table(string name) => AssetDatabase.LoadAssetAtPath<LootTable>($"Assets/_Project/Data/Loot/{name}.asset");
        private static AffixDatabase Affixes => AssetDatabase.LoadAssetAtPath<AffixDatabase>("Assets/_Project/Data/AffixDatabase.asset");

        private static LootRoller Roller(ulong seed) => new LootRoller(new ItemGenerator(Affixes.Affixes, RarityTable.Default), seed);

        [Test, Description("La pozione di cura della scheda (D14): 1 × 1, metà della vita massima, intera e almeno 1; non si indossa")]
        public void HealingPotion_MatchesTheScheda()
        {
            var potion = Healing;

            Assert.AreEqual(Vector2Int.one, potion.Size);
            Assert.AreEqual(0.5f, potion.HealFraction);
            Assert.AreEqual(EquipSlot.None, potion.Slot);
            Assert.AreEqual(AffixTargets.None, potion.AffixTarget);
            Assert.AreEqual(50f, potion.HealAmount(100f));
            Assert.AreEqual(66f, potion.HealAmount(132f));
            Assert.AreEqual(1f, potion.HealAmount(1f), "almeno 1");
            Assert.AreEqual(potion.HealAmount(77f), Mathf.Round(potion.HealAmount(77f)), "intera, come i danni");
        }

        [Test, Description("Su 10.000 tiri la pozione cade da circa il 25% degli scheletri e dal 50% delle casse")]
        public void PotionChances_FollowTheTables()
        {
            var roller = Roller(99UL);
            var skeleton = Table("SkeletonLoot");
            var chest = Table("ChestLoot");
            int skeletons = 0, chests = 0;
            for (int i = 0; i < 10000; i++)
            {
                var potion = roller.RollPotion(skeleton, 1 + i % 4, i * 7, -i);
                skeletons += potion != null ? 1 : 0;
                chests += roller.RollPotion(chest, 1 + i % 4, i * 7, -i) != null ? 1 : 0;
                if (potion != null)
                {
                    Assert.AreSame(Healing, potion.Definition);
                    Assert.AreEqual(Rarity.Normal, potion.Rarity);
                }
            }

            Assert.AreEqual(0.25, skeletons / 10000.0, 0.02);
            Assert.AreEqual(0.5, chests / 10000.0, 0.02);
        }

        [Test, Description("Il tiro della pozione ha un seme suo: stabile, diverso da quello dell'oggetto e scollegato dal drop dell'oggetto")]
        public void PotionRoll_IsSeparateFromTheItem()
        {
            Assert.AreEqual(SeedMixer.ForPotion(4711UL, 2, 30, -10), SeedMixer.ForPotion(4711UL, 2, 30, -10));
            Assert.AreNotEqual(SeedMixer.ForEnemy(4711UL, 2, 30, -10), SeedMixer.ForPotion(4711UL, 2, 30, -10));

            // se i due tiri fossero legati, la pozione cadrebbe più spesso con l'oggetto o senza
            var roller = Roller(7UL);
            var skeleton = Table("SkeletonLoot");
            int withItem = 0, potionWithItem = 0, withoutItem = 0, potionWithoutItem = 0;
            for (int i = 0; i < 10000; i++)
            {
                bool item = roller.Roll(skeleton, 1, i, i * 3) != null;
                bool potion = roller.RollPotion(skeleton, 1, i, i * 3) != null;
                if (item)
                {
                    withItem++;
                    potionWithItem += potion ? 1 : 0;
                }
                else
                {
                    withoutItem++;
                    potionWithoutItem += potion ? 1 : 0;
                }
            }

            Assert.AreEqual(0.25, potionWithItem / (double)withItem, 0.03);
            Assert.AreEqual(0.25, potionWithoutItem / (double)withoutItem, 0.03);
        }

        [Test, Description("Senza pozione nella tabella, o senza tabella, non cade niente")]
        public void RollPotion_WithoutPotion_IsNull()
        {
            var table = ScriptableObject.CreateInstance<LootTable>();
            try
            {
                Assert.IsNull(Roller(1UL).RollPotion(table, 1, 0, 0));
                Assert.IsNull(Roller(1UL).RollPotion(null, 1, 0, 0));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test, Description("Il tooltip della pozione dice quanto cura e come si beve, nelle due lingue")]
        public void Tooltip_DescribesThePotion()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            var localizer = new Localizer(StringTable.Parse(csv.text));
            var builder = new StringBuilder();

            ItemDescription.Write(builder, new ItemInstance(Healing), true, localizer);
            StringAssert.Contains("Healing Potion", builder.ToString());
            StringAssert.Contains("Restores 50% of life", builder.ToString());
            StringAssert.Contains("Right-click", builder.ToString());

            localizer.SetLanguage("it");
            ItemDescription.Write(builder, new ItemInstance(Healing), true, localizer);
            StringAssert.Contains("Pozione di cura", builder.ToString());
            StringAssert.Contains("Rende il 50% della vita", builder.ToString());
            StringAssert.Contains("Click destro", builder.ToString());
        }
    }
}

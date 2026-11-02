using System.Linq;
using System.Text;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Magic;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le pergamene della M9, uscite dal gioco con la M20 (D4): restano nel database perché un
    /// salvataggio vecchio le ritrovi e le venda. Qui anche la pozione di mana, nata con loro.
    /// </summary>
    public class ScrollTests
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/" + path);
        private static ScrollDefinition[] Scrolls => new[] { Asset<ScrollDefinition>("Data/Items/ScrollFireRing.asset"), Asset<ScrollDefinition>("Data/Items/ScrollStoneSkin.asset") };
        private static SpellDefinition FireRing => Asset<SpellDefinition>("Data/Spells/FireRing.asset");
        private static SpellDefinition StoneSkin => Asset<SpellDefinition>("Data/Spells/StoneSkin.asset");
        private static AffixDatabase Affixes => Asset<AffixDatabase>("Data/AffixDatabase.asset");

        private static LootRoller Roller(ulong seed) => new LootRoller(new ItemGenerator(Affixes.Affixes, RarityTable.Default), seed);

        private static Localizer English()
        {
            var localizer = new Localizer(StringTable.Parse(Asset<TextAsset>("Data/Localization/Strings.csv").text));
            localizer.SetLanguage("en");
            return localizer;
        }

        [Test, Description("Le due pergamene di prima della M20, una per l'Anello di fuoco e una per la Pelle di pietra, restano nel database per i salvataggi vecchi: 1 × 2 celle, non si indossano")]
        public void Scrolls_StayInTheDatabase()
        {
            var scrolls = Scrolls;
            CollectionAssert.AreEquivalent(new[] { FireRing, StoneSkin }, scrolls.Select(s => s.Spell).ToArray());
            var database = Asset<ItemDatabase>("Data/ItemDatabase.asset");
            foreach (var scroll in scrolls)
            {
                Assert.AreEqual(new Vector2Int(1, 2), scroll.Size, scroll.name);
                Assert.AreEqual(EquipSlot.None, scroll.Slot);
                Assert.IsTrue(database.TryGet(scroll.Id, out var found) && found == scroll, scroll.name + " nel database");
                Assert.IsNotNull(scroll.Icon, scroll.name + " ha l'icona");
            }
        }

        [Test, Description("La pozione di mana sta nel tratto dopo quella di cura: la pozione di cura cade come prima (trappola 4), e su 10.000 scheletri la blu cade da circa il 5%")]
        public void ManaPotion_FollowsTheHealingPotion()
        {
            var skeleton = Asset<LootTable>("Data/Loot/SkeletonLoot.asset");
            var mana = Asset<PotionDefinition>("Data/Items/ManaPotion.asset");
            Assert.AreSame(skeleton.Potion, skeleton.PotionFor(0.0));
            Assert.AreSame(skeleton.Potion, skeleton.PotionFor(skeleton.PotionChance - 0.001));
            Assert.AreSame(mana, skeleton.PotionFor(skeleton.PotionChance + 0.001));
            Assert.IsNull(skeleton.PotionFor(skeleton.PotionChance + skeleton.ManaPotionChance + 0.001));

            var roller = Roller(7UL);
            int blue = 0;
            for (int i = 0; i < 10000; i++)
            {
                if (roller.RollPotion(skeleton, 1, i, -i)?.Definition == mana)
                {
                    blue++;
                }
            }

            Assert.That(blue / 10000f, Is.InRange(0.04f, 0.06f));
            Assert.AreEqual(PotionKind.Mana, mana.Kind);
            Assert.AreEqual(25f, mana.RestoreAmount(50f), "metà del mana massimo");
        }

        [Test, Description("Tooltip: la pergamena ha solo il nome, non si legge più (M20); la pozione blu dice che rende il mana")]
        public void Tooltips_DescribeScrollAndManaPotion()
        {
            var localizer = English();
            var builder = new StringBuilder();
            ItemDescription.Write(builder, new ItemInstance(Asset<ScrollDefinition>("Data/Items/ScrollFireRing.asset")), true, localizer);
            StringAssert.Contains("Scroll of Ring of Fire", builder.ToString());
            StringAssert.DoesNotContain("Teaches", builder.ToString());
            StringAssert.DoesNotContain("Right-click", builder.ToString());

            ItemDescription.Write(builder, new ItemInstance(Asset<PotionDefinition>("Data/Items/ManaPotion.asset")), true, localizer);
            StringAssert.Contains("Restores 50% of mana", builder.ToString());
        }
    }
}

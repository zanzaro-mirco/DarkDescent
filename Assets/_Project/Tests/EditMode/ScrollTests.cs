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
    public class ScrollTests
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/" + path);
        private static ScrollTable Table => Asset<ScrollTable>("Data/Loot/ScrollTable.asset");
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

        [Test, Description("Le due pergamene della scheda (D5): 1 × 2 celle, una per l'Anello di fuoco e una per la Pelle di pietra, nel database; non si indossano")]
        public void Scrolls_MatchTheScheda()
        {
            var scrolls = Table.Scrolls;
            CollectionAssert.AreEquivalent(new[] { FireRing, StoneSkin }, scrolls.Select(s => s.Spell).ToArray());
            var database = Asset<ItemDatabase>("Data/ItemDatabase.asset");
            foreach (var scroll in scrolls)
            {
                Assert.AreEqual(new Vector2Int(1, 2), scroll.Size, scroll.name);
                Assert.AreEqual(EquipSlot.None, scroll.Slot);
                Assert.IsTrue(database.TryGet(scroll.Id, out var found) && found == scroll, scroll.name + " nel database");
                Assert.IsNotNull(scroll.Icon, scroll.name + " ha l'icona");
            }

            Assert.AreEqual(2, Table.FirstDepth);
        }

        [Test, Description("Niente pergamene prima della profondità 2; dalla 2, su 3000 partite, circa un livello su tre ne ha una (tra 28% e 40%), sempre di un incantesimo voluto")]
        public void ScrollChance_IsAboutOneLevelInThree()
        {
            int found = 0;
            const int runs = 3000;
            for (ulong seed = 1; seed <= runs; seed++)
            {
                var roller = Roller(seed);
                Assert.IsNull(roller.RollScroll(Table, 1, null, out _), "profondità 1");
                var scroll = roller.RollScroll(Table, 3, spell => spell != FireRing, out double carrier);
                if (scroll != null)
                {
                    found++;
                    Assert.AreSame(StoneSkin, ((ScrollDefinition)scroll.Definition).Spell, "l'Anello è già noto: non cade");
                    Assert.That(carrier, Is.InRange(0.0, 1.0));
                }
            }

            Assert.That(found / (float)runs, Is.InRange(0.28f, 0.40f));
        }

        [Test, Description("Con tutti e due gli incantesimi noti non cade nessuna pergamena; lo stesso seme dà sempre la stessa pergamena e lo stesso portatore")]
        public void ScrollRoll_IsDeterministicAndSkipsKnownSpells()
        {
            for (ulong seed = 1; seed <= 300; seed++)
            {
                Assert.IsNull(Roller(seed).RollScroll(Table, 4, spell => false, out _));
                var first = Roller(seed).RollScroll(Table, 4, null, out double carrierA);
                var second = Roller(seed).RollScroll(Table, 4, null, out double carrierB);
                Assert.AreEqual(first?.Definition, second?.Definition);
                Assert.AreEqual(carrierA, carrierB);
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

        [Test, Description("Tooltip: la pergamena dice quale incantesimo insegna e come si legge; la pozione blu dice che rende il mana")]
        public void Tooltips_DescribeScrollAndManaPotion()
        {
            var localizer = English();
            var builder = new StringBuilder();
            ItemDescription.Write(builder, new ItemInstance(Asset<ScrollDefinition>("Data/Items/ScrollFireRing.asset")), true, localizer);
            StringAssert.Contains("Scroll of Ring of Fire", builder.ToString());
            StringAssert.Contains("Teaches Ring of Fire", builder.ToString());
            StringAssert.Contains("Right-click to read", builder.ToString());

            ItemDescription.Write(builder, new ItemInstance(Asset<PotionDefinition>("Data/Items/ManaPotion.asset")), true, localizer);
            StringAssert.Contains("Restores 50% of mana", builder.ToString());
        }
    }
}

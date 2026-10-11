using System.Text;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Stats;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class MagicAffixTests
    {
        private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/Data/" + path);
        private static AffixDefinition Arcane => Asset<AffixDefinition>("Affixes/Arcane.asset");
        private static AffixDefinition OfSorcery => Asset<AffixDefinition>("Affixes/OfSorcery.asset");

        [Test, Description("Arcano (+5–15 mana) e della Stregoneria (+2–5 Magia, dal livello 2) vanno solo su gioielli ed elmi, e sono nel database degli affissi")]
        public void MagicAffixes_OnlyOnJewelryAndHelms()
        {
            var database = Asset<AffixDatabase>("AffixDatabase.asset");
            CollectionAssert.Contains(database.Affixes, Arcane);
            CollectionAssert.Contains(database.Affixes, OfSorcery);
            Assert.AreEqual((AffixEffect.Mana, 5, 15), (Arcane.Effect, Arcane.Min, Arcane.Max));
            Assert.AreEqual((AffixEffect.Magic, 2, 5), (OfSorcery.Effect, OfSorcery.Min, OfSorcery.Max));

            foreach (var allowed in new[] { "Ring", "Amulet", "KnightHelm", "BearHat" })
            {
                var item = Asset<ItemDefinition>($"Items/{allowed}.asset");
                Assert.IsTrue(Arcane.AllowedOn(item, 2), allowed);
                Assert.IsTrue(OfSorcery.AllowedOn(item, 2), allowed);
            }

            foreach (var refused in new[] { "Breastplate", "IronBoots", "LeatherGloves", "ShortSword", "RoundShield" })
            {
                var item = Asset<ItemDefinition>($"Items/{refused}.asset");
                Assert.IsFalse(Arcane.AllowedOn(item, 5), refused);
                Assert.IsFalse(OfSorcery.AllowedOn(item, 5), refused);
            }

            Assert.IsFalse(OfSorcery.AllowedOn(Asset<ItemDefinition>("Items/Ring.asset"), 1), "dal livello 2");

            // gli affissi di prima vanno ancora sugli elmi, che restano armature
            Assert.IsTrue(Asset<AffixDefinition>("Affixes/Sturdy.asset").AllowedOn(Asset<ItemDefinition>("Items/KnightHelm.asset"), 5));
        }

        [Test, Description("Un anello Arcano della Stregoneria indossato alza Magia e mana sulla scheda; il tooltip lo dice")]
        public void MagicAffixes_RaiseMagicAndMana()
        {
            var sheet = new StatSheet();
            var equipment = new Equipment(sheet);
            var ring = new ItemInstance(Asset<ItemDefinition>("Items/Ring.asset"), Rarity.Magic, 3, 1UL,
                new[] { new ItemAffix(Arcane, 12), new ItemAffix(OfSorcery, 4) });
            Assert.IsTrue(equipment.TryEquip(ring, out _));
            Assert.AreEqual(12f, sheet.Get(StatType.Mana));
            Assert.AreEqual(4f, sheet.Get(StatType.Magic));
            Assert.AreEqual(10f + 2f * 4f + 12f, CombatFormulas.MaxMana(sheet.Get(StatType.Magic), 1f, sheet.Get(StatType.Mana)));

            var localizer = new Localizer(StringTable.Parse(Asset<TextAsset>("Localization/Strings.csv").text));
            var builder = new StringBuilder();
            ItemDescription.Write(builder, ring, true, localizer);
            StringAssert.Contains("+12 mana", builder.ToString());
            StringAssert.Contains("+4 Magic", builder.ToString());
            StringAssert.Contains("Arcane Ring of Sorcery", builder.ToString());
            localizer.SetLanguage("it");
            ItemDescription.Write(builder, ring, true, localizer);
            StringAssert.Contains("+4 Magia", builder.ToString());
        }
    }
}

using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Le basi della M8 (D5 e D6): due elmi, tre armature, due guanti, due stivali, l'anello e
    /// l'amuleto, con il loro slot, la loro Armatura e la Forza richiesta; gli affissi che ci vanno;
    /// il critico dei gioielli; i nomi al plurale di guanti e stivali.
    /// </summary>
    public class M8ItemsTests
    {
        private static T Item<T>(string name) where T : ItemDefinition
        {
            return AssetDatabase.LoadAssetAtPath<T>($"Assets/_Project/Data/Items/{name}.asset");
        }

        private static AffixDefinition Affix(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AffixDefinition>($"Assets/_Project/Data/Affixes/{name}.asset");
        }

        private static Localizer Localizer(string language)
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            var localizer = new Localizer(StringTable.Parse(csv.text));
            localizer.SetLanguage(language);
            return localizer;
        }

        [Test, Description("Le undici basi nuove hanno slot, Armatura e Forza richiesta della scheda, un modello e un'icona")]
        public void M8Bases_MatchTheScheda()
        {
            var armor = new (string name, EquipSlot slot, int armor, int strength)[]
            {
                ("BearHat", EquipSlot.Helm, 3, 0),
                ("KnightHelm", EquipSlot.Helm, 6, 25),
                ("LeatherArmor", EquipSlot.Body, 8, 0),
                ("Breastplate", EquipSlot.Body, 14, 35),
                ("DarkPlate", EquipSlot.Body, 20, 50),
                ("LeatherGloves", EquipSlot.Gloves, 2, 0),
                ("Gauntlets", EquipSlot.Gloves, 4, 25),
                ("LeatherBoots", EquipSlot.Boots, 2, 0),
                ("IronBoots", EquipSlot.Boots, 5, 25),
            };
            foreach (var (name, slot, value, strength) in armor)
            {
                var item = Item<ArmorDefinition>(name);
                Assert.IsNotNull(item, name);
                Assert.AreEqual((slot, value, strength, 0), (item.Slot, item.Armor, item.RequiredStrength, item.BlockChance), name);
                Assert.IsNotNull(item.Model, name);
                Assert.IsNotNull(item.Icon, name);
            }

            Assert.AreEqual(EquipSlot.Ring, Item<JewelryDefinition>("Ring").Slot);
            Assert.AreEqual(EquipSlot.Amulet, Item<JewelryDefinition>("Amulet").Slot);
            Assert.AreEqual(Vector2Int.one, Item<JewelryDefinition>("Ring").Size);
        }

        [Test, Description("Gli affissi della D5: Armatura sui pezzi d'armatura, attributi e vita ovunque, colpire e critico sui gioielli, il danno solo sulle armi")]
        public void Affixes_GoWhereTheSchedaSays()
        {
            var helm = new ItemInstance(Item<ArmorDefinition>("KnightHelm")).Definition;
            var ring = new ItemInstance(Item<JewelryDefinition>("Ring")).Definition;
            Assert.IsTrue(Affix("Sturdy").AllowedOn(helm, 1));
            Assert.IsTrue(Affix("Massive").AllowedOn(helm, 2));
            Assert.IsFalse(Affix("Sturdy").AllowedOn(ring, 2));
            Assert.IsTrue(Affix("OfTheBear").AllowedOn(ring, 1));
            Assert.IsTrue(Affix("OfStrength").AllowedOn(helm, 1));
            Assert.IsTrue(Affix("Accurate").AllowedOn(ring, 1));
            Assert.IsTrue(Affix("Deadly").AllowedOn(ring, 2));
            Assert.IsFalse(Affix("Deadly").AllowedOn(ring, 1), "il critico dal livello 2");
            Assert.IsFalse(Affix("Deadly").AllowedOn(helm, 2));
            Assert.IsFalse(Affix("Sharp").AllowedOn(ring, 2));
            Assert.IsFalse(Affix("OfBlocking").AllowedOn(helm, 2), "il blocco resta degli scudi");
        }

        [Test, Description("Un anello Letale +4% alza la probabilità di critico; toglierlo la riporta com'era; il massimo resta 50")]
        public void DeadlyRing_RaisesCritChance()
        {
            var stats = new StatSheet();
            stats.SetBase(StatType.Dexterity, 20f);
            var equipment = new Equipment(stats);
            var ring = new ItemInstance(Item<JewelryDefinition>("Ring"), Rarity.Magic, 2, 1UL, new[] { new ItemAffix(Affix("Deadly"), 4) });

            Assert.AreEqual(7f, CombatFormulas.CritChance(20f, stats.Get(StatType.CritChance)));
            Assert.IsTrue(equipment.TryEquip(ring, out _));
            Assert.AreEqual(11f, CombatFormulas.CritChance(20f, stats.Get(StatType.CritChance)));
            Assert.AreEqual(50f, CombatFormulas.CritChance(600f, stats.Get(StatType.CritChance)));
            equipment.Unequip(EquipSlot.Ring);
            Assert.AreEqual(7f, CombatFormulas.CritChance(20f, stats.Get(StatType.CritChance)));
        }

        [Test, Description("In italiano guanti e stivali vogliono il prefisso al plurale: Guanti di ferro Robusti, Stivali di cuoio Massicci della Forza")]
        public void PluralBases_AgreeInItalian()
        {
            string Name(string item, string language, params string[] affixes)
            {
                var instance = new ItemInstance(Item<ArmorDefinition>(item), Rarity.Magic, 2, 1UL, affixes.Select(a => new ItemAffix(Affix(a), 2)).ToArray());
                return ItemNamer.Name(instance, Localizer(language));
            }

            Assert.AreEqual("Guanti di ferro Robusti", Name("Gauntlets", "it", "Sturdy"));
            Assert.AreEqual("Stivali di cuoio Massicci della Forza", Name("LeatherBoots", "it", "Massive", "OfStrength"));
            Assert.AreEqual("Armatura di cuoio Robusta", Name("LeatherArmor", "it", "Sturdy"));
            Assert.AreEqual("Sturdy Gauntlets", Name("Gauntlets", "en", "Sturdy"));
        }
    }
}

using System.Linq;
using DarkDescent.Items;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class ItemNamerTests
    {
        private Localizer _localizer;

        private static ItemInstance With(string item, params string[] affixes)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{item}.asset");
            var rolled = affixes.Select(a => new ItemAffix(AssetDatabase.LoadAssetAtPath<AffixDefinition>($"Assets/_Project/Data/Affixes/{a}.asset"), 1)).ToArray();
            return new ItemInstance(definition, rolled.Length == 0 ? Rarity.Normal : Rarity.Magic, 1, 1UL, rolled);
        }

        private string Name(ItemInstance item, string language)
        {
            _localizer.SetLanguage(language);
            return ItemNamer.Name(item, _localizer);
        }

        [SetUp]
        public void SetUp()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            _localizer = new Localizer(StringTable.Parse(csv.text));
        }

        [Test, Description("Il prefisso si accorda al genere della base in italiano; in inglese va prima del nome")]
        public void Prefix_AgreesWithGender()
        {
            Assert.AreEqual("Sturdy Axe", Name(With("Axe", "Sturdy"), "en"));
            Assert.AreEqual("Ascia Robusta", Name(With("Axe", "Sturdy"), "it"));
            Assert.AreEqual("Sturdy Crest Shield", Name(With("BadgeShield", "Sturdy"), "en"));
            Assert.AreEqual("Scudo con stemma Robusto", Name(With("BadgeShield", "Sturdy"), "it"));
            Assert.AreEqual("Pugnale Affilato", Name(With("Dagger", "Sharp"), "it"));
        }

        [Test, Description("Prefisso e suffisso insieme, solo suffisso, nessun affisso")]
        public void Pattern_HandlesMissingParts()
        {
            Assert.AreEqual("Savage Short Sword of the Griffin", Name(With("ShortSword", "Savage", "OfTheGriffin"), "en"));
            Assert.AreEqual("Spada corta Feroce del Grifone", Name(With("ShortSword", "Savage", "OfTheGriffin"), "it"));
            Assert.AreEqual("Axe of the Bear", Name(With("Axe", "OfTheBear"), "en"));
            Assert.AreEqual("Ascia dell'Orso", Name(With("Axe", "OfTheBear"), "it"));
            Assert.AreEqual("Skeleton Blade", Name(With("SkeletonBlade"), "en"));
        }

        [Test, Description("Un raro si chiama con il primo prefisso e il primo suffisso")]
        public void Rare_UsesFirstPrefixAndSuffix()
        {
            var rare = With("SquareShield", "Sturdy", "OfStrength", "OfBlocking");

            Assert.AreEqual("Sturdy Square Shield of Strength", Name(rare, "en"));
            Assert.AreEqual("Scudo quadrato Robusto della Forza", Name(rare, "it"));
        }
    }
}

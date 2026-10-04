using System;
using System.Linq;
using System.Text;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class ItemDescriptionTests
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private Localizer _localizer;

        private static ItemDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset");
        private static AffixDefinition Affix(string name) => AssetDatabase.LoadAssetAtPath<AffixDefinition>($"Assets/_Project/Data/Affixes/{name}.asset");

        private static ItemInstance Normal(string name) => new ItemInstance(Load(name));

        private static ItemInstance With(string name, Rarity rarity, params (string affix, int value)[] affixes)
        {
            return new ItemInstance(Load(name), rarity, 1, 1UL, affixes.Select(a => new ItemAffix(Affix(a.affix), a.value)).ToArray());
        }

        [SetUp]
        public void SetUp()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            _localizer = new Localizer(StringTable.Parse(csv.text));
        }

        private string Write(ItemInstance item, bool meets = true, string header = null)
        {
            ItemDescription.Write(_builder, item, meets, _localizer, header);
            return _builder.ToString();
        }

        [Test, Description("La lama: nome, danno e Forza richiesta, senza rosso se la Forza basta")]
        public void Blade_ShowsDamageAndRequirement()
        {
            string text = Write(Normal("SkeletonBlade"));

            StringAssert.Contains("Skeleton Blade", text);
            StringAssert.Contains("Damage: 8–12", text);
            StringAssert.Contains("Required Strength: 25", text);
            StringAssert.DoesNotContain(ItemDescription.UnmetColor, text);
        }

        [Test, Description("Con la Forza che non basta, il requisito è in rosso")]
        public void UnmetRequirement_IsRed()
        {
            StringAssert.Contains($"<color={ItemDescription.UnmetColor}>Required Strength: 25</color>", Write(Normal("SkeletonBlade"), meets: false));
        }

        [Test, Description("Uno scudo normale: nome bianco, Armatura e blocco; nessuna riga di affissi")]
        public void NormalShield_ShowsArmorAndBlock()
        {
            Assert.AreEqual($"<b><color={RarityColors.TextHex(Rarity.Normal)}>Crest Shield</color></b>\nArmor: 5\nBlock chance: 10%", Write(Normal("BadgeShield")));
        }

        [Test, Description("Spada corta Affilata +40%: nome blu, danno già modificato 8–12 e la riga dell'affisso")]
        public void SharpSword_ShowsModifiedDamageAndAffixLine()
        {
            string text = Write(With("ShortSword", Rarity.Magic, ("Sharp", 40)));

            StringAssert.Contains($"<color={RarityColors.TextHex(Rarity.Magic)}>Sharp Short Sword</color>", text);
            StringAssert.Contains("Damage: 8–12", text);
            StringAssert.Contains($"<color={ItemDescription.AffixColor}>+40% damage</color>", text);
        }

        [Test, Description("In italiano lo stesso tooltip è tradotto e accordato, con il titolo del confronto")]
        public void Italian_TranslatesAndAgrees()
        {
            _localizer.SetLanguage("it");

            string text = Write(With("BadgeShield", Rarity.Rare, ("Massive", 50), ("OfBlocking", 10), ("OfTheBear", 8)), header: "Equipaggiato");

            StringAssert.StartsWith("<size=80%>", text);
            StringAssert.Contains("Equipaggiato", text);
            StringAssert.Contains("Scudo con stemma Massiccio della Parata", text);
            StringAssert.Contains("Armatura: 7", text);
            StringAssert.Contains("Blocco: 20%", text);
            StringAssert.Contains("+8 vita", text);
        }

        [Test, Description("Ogni effetto degli affissi ha la sua riga nella tabella")]
        public void EveryEffect_HasALine()
        {
            foreach (AffixEffect effect in Enum.GetValues(typeof(AffixEffect)))
            {
                StringAssert.DoesNotStartWith("#", _localizer.Get(ItemDescription.EffectKey(effect)), effect.ToString());
            }

            Assert.AreEqual(Enum.GetValues(typeof(AffixEffect)).Length,
                Enum.GetValues(typeof(AffixEffect)).Cast<AffixEffect>().Select(ItemDescription.EffectKey).Distinct().Count(), "una chiave per effetto");
        }
    }
}

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

        [SetUp]
        public void SetUp()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            _localizer = new Localizer(StringTable.Parse(csv.text));
        }

        [Test, Description("La lama: nome, danno e Forza richiesta, senza rosso se la Forza basta")]
        public void Blade_ShowsDamageAndRequirement()
        {
            ItemDescription.Write(_builder, Load("SkeletonBlade"), meetsRequirements: true, _localizer);
            string text = _builder.ToString();

            StringAssert.Contains("Skeleton Blade", text);
            StringAssert.Contains("Damage: 8–12", text);
            StringAssert.Contains("Required Strength: 25", text);
            StringAssert.DoesNotContain("<color", text);
        }

        [Test, Description("Con la Forza che non basta, il requisito è in rosso")]
        public void UnmetRequirement_IsRed()
        {
            ItemDescription.Write(_builder, Load("SkeletonBlade"), meetsRequirements: false, _localizer);

            StringAssert.Contains($"<color={ItemDescription.UnmetColor}>Required Strength: 25</color>", _builder.ToString());
        }

        [Test, Description("La spada corta non chiede Forza, lo scudo mostra l'Armatura; il testo vecchio sparisce")]
        public void SwordAndShield_ShowTheirLines()
        {
            ItemDescription.Write(_builder, Load("ShortSword"), meetsRequirements: true, _localizer);
            StringAssert.Contains("Damage: 6–9", _builder.ToString());
            StringAssert.DoesNotContain("Strength", _builder.ToString());

            ItemDescription.Write(_builder, Load("BadgeShield"), meetsRequirements: true, _localizer);
            Assert.AreEqual("<b>Crest Shield</b>\nArmor: 5", _builder.ToString());
        }

        [Test, Description("In italiano lo stesso tooltip è tradotto, numeri compresi")]
        public void Italian_TranslatesEveryLine()
        {
            _localizer.SetLanguage("it");

            ItemDescription.Write(_builder, Load("SkeletonBlade"), meetsRequirements: false, _localizer);

            Assert.AreEqual($"<b>Lama dello scheletro</b>\nDanno: 8–12\n<color={ItemDescription.UnmetColor}>Forza richiesta: 25</color>", _builder.ToString());
        }
    }
}

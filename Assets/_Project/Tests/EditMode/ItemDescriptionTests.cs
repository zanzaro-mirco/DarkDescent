using System.Text;
using DarkDescent.Items;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class ItemDescriptionTests
    {
        private readonly StringBuilder _builder = new StringBuilder();

        private static ItemDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Data/Items/{name}.asset");

        [Test, Description("La lama: nome, danno e Forza richiesta, senza rosso se la Forza basta")]
        public void Blade_ShowsDamageAndRequirement()
        {
            ItemDescription.Write(_builder, Load("SkeletonBlade"), meetsRequirements: true);
            string text = _builder.ToString();

            StringAssert.Contains("Lama dello scheletro", text);
            StringAssert.Contains("Danno: 8–12", text);
            StringAssert.Contains("Forza richiesta: 25", text);
            StringAssert.DoesNotContain("<color", text);
        }

        [Test, Description("Con la Forza che non basta, il requisito è in rosso")]
        public void UnmetRequirement_IsRed()
        {
            ItemDescription.Write(_builder, Load("SkeletonBlade"), meetsRequirements: false);

            StringAssert.Contains($"<color={ItemDescription.UnmetColor}>Forza richiesta: 25</color>", _builder.ToString());
        }

        [Test, Description("La spada corta non chiede Forza, lo scudo mostra l'Armatura; il testo vecchio sparisce")]
        public void SwordAndShield_ShowTheirLines()
        {
            ItemDescription.Write(_builder, Load("ShortSword"), meetsRequirements: true);
            StringAssert.Contains("Danno: 6–9", _builder.ToString());
            StringAssert.DoesNotContain("Forza", _builder.ToString());

            ItemDescription.Write(_builder, Load("BadgeShield"), meetsRequirements: true);
            Assert.AreEqual("<b>Scudo con stemma</b>\nArmatura: 5", _builder.ToString());
        }
    }
}

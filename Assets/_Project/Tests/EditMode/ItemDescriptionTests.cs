using System;
using System.Linq;
using System.Text;
using DarkDescent.Items;
using DarkDescent.Localization;
using DarkDescent.Stats;
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

        private static UniqueDefinition Named(string path) => AssetDatabase.LoadAssetAtPath<UniqueDefinition>($"Assets/_Project/Data/Named/{path}.asset");

        // la riga di un effetto come la scrive il tooltip, nel suo colore e con l'aggiunta dopo il valore
        private string Line(string color, AffixEffect effect, int value, string after = null)
        {
            string text = string.Format(System.Globalization.CultureInfo.InvariantCulture, _localizer.Get(ItemDescription.EffectKey(effect)), value);
            return $"\n<color={color}>{text}{(after == null ? string.Empty : " " + after)}</color>";
        }

        private static Equipment Knight()
        {
            var stats = new StatSheet();
            stats.SetBase(StatType.Strength, 60f);
            stats.SetBase(StatType.Dexterity, 40f);
            return new Equipment(stats);
        }

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

        [Test, Description("Un nome lungo si divide allo spazio più vicino al centro (M9, D1); un nome senza spazi resta com'è; il tooltip lo chiede con twoLineName")]
        public void LongName_SplitsAtMiddle()
        {
            Assert.AreEqual("Lama dello scheletro\nPrecisa della Vitalità", ItemDescription.SplitAtMiddle("Lama dello scheletro Precisa della Vitalità"));
            Assert.AreEqual("Accurate Skeleton\nBlade of Vitality", ItemDescription.SplitAtMiddle("Accurate Skeleton Blade of Vitality"));
            Assert.AreEqual("Ascia", ItemDescription.SplitAtMiddle("Ascia"));

            var item = With("SkeletonBlade", Rarity.Magic, ("Accurate", 5), ("OfVitality", 3));
            ItemDescription.Write(_builder, item, true, _localizer, null, twoLineName: true);
            StringAssert.Contains("Accurate Skeleton\nBlade of Vitality</color>", _builder.ToString());
            ItemDescription.Write(_builder, item, true, _localizer);
            StringAssert.Contains("Accurate Skeleton Blade of Vitality</color>", _builder.ToString(), "di base su una riga");
        }

        [Test, Description("La lama: nome, danno e Forza richiesta, senza rosso se la Forza basta")]
        public void Blade_ShowsDamageAndRequirement()
        {
            string text = Write(Normal("SkeletonBlade"));

            StringAssert.Contains("Skeleton Blade", text);
            StringAssert.Contains("Damage: 8–12", text);
            StringAssert.Contains("Attack speed: Slow", text);
            StringAssert.Contains("Required Strength: 25", text);
            StringAssert.DoesNotContain(ItemDescription.UnmetColor, text);
        }

        [Test, Description("La velocità d'attacco a parole: il pugnale veloce, la spada corta normale, l'ascia lenta; in italiano tradotta")]
        public void Weapons_ShowTheirSpeed()
        {
            StringAssert.Contains("Attack speed: Fast", Write(Normal("Dagger")));
            StringAssert.Contains("Attack speed: Normal", Write(Normal("ShortSword")));
            StringAssert.Contains("Attack speed: Slow", Write(Normal("Axe")));
            _localizer.SetLanguage("it");
            StringAssert.Contains("Velocità d'attacco: Veloce", Write(Normal("Dagger")));
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

            StringAssert.StartsWith("<size=" + ItemDescription.HeaderSize + "><color=" + ItemDescription.HeaderColor + ">Equipaggiato", text);
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

        [Test, Description("Un unico: il suo nome nel colore dell'oro scuro, sotto la base in piccolo, poi le proprietà in blu senza grado e senza set")]
        public void Unique_ShowsNameBaseAndProperties()
        {
            var fang = ItemGenerator.GenerateNamed(Named("Uniques/MorvaksFang"), 30, 3UL);
            string text = Write(fang);

            string gold = RarityColors.TextHex(Rarity.Unique);
            StringAssert.StartsWith($"<b><color={gold}>Morvak's Fang</color></b>\n<size=85%><color={gold}>Dagger</color></size>\n", text);
            StringAssert.Contains("Attack speed: Fast", text);
            for (int i = 0; i < fang.Named.Properties.Count; i++)
            {
                StringAssert.Contains(Line(ItemDescription.AffixColor, fang.Named.Properties[i].Effect, fang.NamedValues[i]), text);
            }

            StringAssert.DoesNotContain(RarityColors.TextHex(Rarity.Set), text);
            StringAssert.DoesNotContain(ItemDescription.GradeSize, text);
        }

        [Test, Description("Un pezzo del Custode nella borsa con due pezzi addosso: il set, i pezzi indossati in bianco e gli altri in grigio, il bonus da 2 acceso in verde al livello del pezzo più basso, gli altri spenti")]
        public void SetPiece_ShowsWornPiecesAndLitBonuses()
        {
            var equipment = Knight();
            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensGauntlets"), 20, 1UL), out _));
            Assert.IsTrue(equipment.TryEquip(ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensBoots"), 10, 2UL), out _));
            var helm = ItemGenerator.GenerateNamed(Named("Sets/Warden/WardensHelm"), 40, 3UL);
            var set = helm.Named.Set;

            ItemDescription.Write(_builder, helm, true, _localizer, equipment: equipment);
            string text = _builder.ToString();

            string green = RarityColors.TextHex(Rarity.Set);
            StringAssert.StartsWith($"<b><color={green}>Warden's Helm</color></b>", text);
            string grey = ItemDescription.InactiveColor;
            string white = ItemDescription.WornPieceColor;
            StringAssert.Contains($"\n\n<color={green}>The Warden's Armor</color>" +
                $"\n<color={grey}>Warden's Helm</color>\n<color={grey}>Warden's Breastplate</color>" +
                $"\n<color={white}>Warden's Gauntlets</color>\n<color={white}>Warden's Boots</color>", text);

            // i valori del set al livello 10, quello degli stivali
            StringAssert.Contains(Line(green, AffixEffect.Life, set.Bonuses[0].Properties[0].ValueAt(10, set.MinLevel), "(2 pieces)"), text);
            StringAssert.Contains(Line(grey, AffixEffect.Armor, set.Bonuses[1].Properties[0].ValueAt(10, set.MinLevel), "(3 pieces)"), text);
            StringAssert.Contains(Line(grey, AffixEffect.DamageReduction, set.Bonuses[2].Properties[0].ValueAt(10, set.MinLevel), "(full set)"), text);
            StringAssert.Contains(Line(grey, AffixEffect.AllAttributes, set.Bonuses[2].Properties[1].ValueAt(10, set.MinLevel), "(full set)"), text);
        }

        [Test, Description("Senza niente addosso tutti i pezzi e i bonus sono grigi, con i valori al livello dell'oggetto; in italiano \"(2 pezzi)\" e \"(set completo)\"")]
        public void SetPiece_NothingWorn_AllGreyAndItalian()
        {
            _localizer.SetLanguage("it");
            var hood = ItemGenerator.GenerateNamed(Named("Sets/Hunter/HuntersHood"), 55, 4UL);
            var set = hood.Named.Set;

            ItemDescription.Write(_builder, hood, true, _localizer, equipment: Knight());
            string text = _builder.ToString();

            string grey = ItemDescription.InactiveColor;
            StringAssert.Contains($"<size=85%><color={RarityColors.TextHex(Rarity.Set)}>Copricapo d'orso</color></size>", text);
            StringAssert.Contains($"\n<color={grey}>Cappuccio del Cacciatore</color>", text);
            StringAssert.DoesNotContain(ItemDescription.WornPieceColor, text);
            StringAssert.Contains(Line(grey, AffixEffect.ToHit, set.Bonuses[0].Properties[0].ValueAt(55, set.MinLevel), "(2 pezzi)"), text);
            StringAssert.Contains(Line(grey, AffixEffect.LifeSteal, set.Bonuses[2].Properties[1].ValueAt(55, set.MinLevel), "(set completo)"), text);
        }
    }
}

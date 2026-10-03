using System;
using DarkDescent.Stats;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class StatSheetTests
    {
        private static StatSheet SheetWithStrength(float strength)
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.Strength, strength);
            return sheet;
        }

        [Test, Description("Senza modificatori il valore è quello base")]
        public void Get_WithoutModifiers_ReturnsBase()
        {
            var sheet = SheetWithStrength(30f);

            Assert.AreEqual(30f, sheet.Get(StatType.Strength));
            Assert.AreEqual(0f, sheet.Get(StatType.Armor));
        }

        [Test, Description("Prima si sommano i fissi, poi si applicano le percentuali: (30 + 10) × 1,5 = 60, non 30 × 1,5 + 10 = 55")]
        public void FlatModifiers_ApplyBeforePercent()
        {
            var sheet = SheetWithStrength(30f);
            var item = new object();

            // l'ordine in cui arrivano non conta
            sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Percent, 50f, item));
            sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Flat, 10f, item));

            Assert.AreEqual(60f, sheet.Get(StatType.Strength), 0.0001f);
        }

        [Test, Description("Le percentuali si sommano tra loro: +20% e +30% fanno +50%, non 1,2 × 1,3 = +56%")]
        public void PercentModifiers_AreAdditive()
        {
            var sheet = SheetWithStrength(100f);
            sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Percent, 20f, new object()));
            sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Percent, 30f, new object()));

            Assert.AreEqual(150f, sheet.Get(StatType.Strength), 0.0001f);
        }

        [Test, Description("Un modificatore cambia solo la sua statistica")]
        public void Modifier_AffectsOnlyItsStat()
        {
            var sheet = SheetWithStrength(30f);
            sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, 5f, new object()));

            Assert.AreEqual(30f, sheet.Get(StatType.Strength));
            Assert.AreEqual(5f, sheet.Get(StatType.Armor));
        }

        [Test, Description("Togliendo una sorgente se ne vanno tutti i suoi modificatori e i valori tornano esattamente quelli di partenza")]
        public void RemoveModifiersFrom_RestoresExactValues()
        {
            var sheet = SheetWithStrength(30f);
            sheet.SetBase(StatType.Dexterity, 20f);
            var sword = new object();
            var shield = new object();
            sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Flat, 7f, sword));
            sheet.AddModifier(new StatModifier(StatType.Strength, ModifierKind.Percent, 13f, sword));
            sheet.AddModifier(new StatModifier(StatType.Dexterity, ModifierKind.Percent, 9f, sword));
            sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, 5f, shield));

            int removed = sheet.RemoveModifiersFrom(sword);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(30f, sheet.Get(StatType.Strength));
            Assert.AreEqual(20f, sheet.Get(StatType.Dexterity));
            Assert.AreEqual(5f, sheet.Get(StatType.Armor), "i modificatori dello scudo restano");
        }

        [Test, Description("Changed scatta a ogni cambio, e non quando si toglie una sorgente che non c'era")]
        public void Changed_RaisedOnlyOnRealChanges()
        {
            var sheet = new StatSheet();
            var source = new object();
            int changes = 0;
            sheet.Changed += () => changes++;

            sheet.SetBase(StatType.Vitality, 25f);
            sheet.AddModifier(new StatModifier(StatType.Vitality, ModifierKind.Flat, 5f, source));
            sheet.RemoveModifiersFrom(source);
            sheet.RemoveModifiersFrom(source);

            Assert.AreEqual(3, changes);
        }

        [Test, Description("Un modificatore senza sorgente non si potrebbe togliere: viene rifiutato")]
        public void AddModifier_WithoutSource_Throws()
        {
            var sheet = new StatSheet();

            Assert.Throws<ArgumentException>(() => sheet.AddModifier(new StatModifier(StatType.Armor, ModifierKind.Flat, 1f, null)));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using DarkDescent.Editor;
using DarkDescent.Progression;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>La misura delle difficoltà (D10 della M14, passo 14.6): le formule e le fasce con gli asset veri.</summary>
    public class DifficultyCensusTests
    {
        private List<DifficultyCensus.Row> _rows;
        private DifficultyTable _table;
        private DifficultyCensus _census;

        [OneTimeSetUp]
        public void Measure()
        {
            _table = AssetDatabase.LoadAssetAtPath<DifficultyTable>(DifficultyCensus.DifficultiesPath);
            _census = new DifficultyCensus(20, 8);
            _rows = _census.Measure(_table);
        }

        private static CensusFighter Fighter(float life, int min, int max, float dexterity = 0f, float armor = 0f, bool canCrit = false, float block = 0f)
        {
            return new CensusFighter(life, min, max, 0f, 0f, dexterity, armor, 0f, 0f, canCrit, block, 1f);
        }

        [Test, Description("Un'arma da 10 fa 10 a colpo; con Destrezza 50 il critico al 10% porta la media a 11; Forza 50 e livello 10 fanno 10 × 1,5 + 5")]
        public void AverageHit_FollowsTheFormulas()
        {
            Assert.AreEqual(10.0, PowerCensus.AverageHit(Fighter(1f, 10, 10)), 1e-9);
            Assert.AreEqual(11.0, PowerCensus.AverageHit(Fighter(1f, 10, 10, dexterity: 50f, canCrit: true)), 1e-9);
            var strong = new CensusFighter(1f, 10, 10, 50f, 10f, 0f, 0f, 0f, 0f, false, 0f, 2f);
            Assert.AreEqual(40.0, PowerCensus.AverageHit(strong), 1e-9, "(15 + 5) per il moltiplicatore 2");
        }

        [Test, Description("Contro Armatura 25 si colpisce al 50%, e uno scudo al 20% lascia passare l'80%: 40 di vita con colpi da 10 vogliono 10 colpi tirati e 4 arrivati")]
        public void Swings_CountMissesAndBlocks()
        {
            var attacker = Fighter(1f, 10, 10);
            var defender = Fighter(40f, 1, 1, armor: 25f, block: 20f);
            Assert.AreEqual(0.4, PowerCensus.LandChance(attacker, defender), 1e-9);
            Assert.AreEqual(10.0, PowerCensus.SwingsToKill(attacker, defender), 1e-9);
            Assert.AreEqual(4.0, PowerCensus.HitsToDie(attacker, defender), 1e-9);
        }

        [Test, Description("La difficoltà moltiplica vita e danno e aggiunge colpire e Armatura al nemico")]
        public void Foe_TakesTheDifficulty()
        {
            var shadow = _table.Tier(1);
            var foe = PowerCensus.Foe(30f, 4, 6, 10f, 10f, shadow);
            Assert.AreEqual(30f * shadow.Life, foe.Life, 1e-4f);
            Assert.AreEqual(shadow.Damage, foe.DamageFactor, 1e-6f);
            Assert.AreEqual((10f + shadow.Armor, (float)shadow.ToHit), (foe.Armor, foe.ToHit));
        }

        [Test, Description("A ogni difficoltà e profondità: scheletro in 1–5 colpi, bruto in 4–14; il cavaliere regge almeno 3 colpi del bruto e 2 del re; colpisce il bruto almeno 2 volte su 3, e il bruto lo colpisce tra il 5% e il 45% delle volte")]
        public void EveryDepth_StaysInTheBands()
        {
            Assert.AreEqual(_table.Count * 8, _rows.Count);
            foreach (var row in _rows)
            {
                string where = $"{row.TierName} profondità {row.Depth}";
                Assert.That(row.SkeletonSwings, Is.InRange(1.0, 5.0), where);
                Assert.That(row.BruteSwings, Is.InRange(4.0, 14.0), where);
                Assert.GreaterOrEqual(row.BruteHitsTaken, 3.0, where);
                Assert.GreaterOrEqual(row.KingHitsTaken, 2.0, where);
                Assert.GreaterOrEqual(row.HeroLandsOnBrute, 0.65, where);
                Assert.That(row.BruteLandsOnHero, Is.InRange(0.05, 0.45), where);
            }
        }

        [Test, Description("Dall'Ombra ogni discesa comincia dura (bruto in 9–14 colpi alla profondità 1), un po' più dura della precedente")]
        public void EachDifficulty_StartsHarder()
        {
            double previous = 0.0;
            for (int t = 1; t < _table.Count; t++)
            {
                var first = _rows.Single(r => r.Tier == t && r.Depth == 1);
                Assert.That(first.BruteSwings, Is.InRange(9.0, 14.0), first.TierName);
                Assert.GreaterOrEqual(first.BruteSwings, previous, first.TierName);
                previous = first.BruteSwings;
            }
        }

        [Test, Description("Il re dura un po' di più a ogni gradino: più colpi del precedente, ma meno di una volta e mezza")]
        public void King_LastsALittleLonger()
        {
            var kings = _rows.Where(r => r.Depth == 8).OrderBy(r => r.Tier).Select(r => r.KingSwings).ToList();
            for (int t = 1; t < kings.Count; t++)
            {
                Assert.Greater(kings[t], kings[t - 1], _table.Tier(t).NameKey);
                Assert.Less(kings[t], kings[t - 1] * 1.5, _table.Tier(t).NameKey);
            }
        }

        [Test, Description("Con una discesa per difficoltà il cavaliere comincia ognuna al più due livelli sotto la zona, e arriva al 50 prima dell'Abisso IV")]
        public void Experience_KeepsUpWithTheZone()
        {
            for (int t = 1; t < _table.Count; t++)
            {
                var first = _rows.Single(r => r.Tier == t && r.Depth == 1);
                int zone = _census.Settings.ZoneLevel(1, _table.Tier(t).ZoneLevelBonus);
                Assert.GreaterOrEqual(first.HeroLevel, System.Math.Min(_census.Settings.MaxLevel, zone - 2), first.TierName);
            }

            Assert.AreEqual(_census.Settings.MaxLevel, _rows.Single(r => r.Tier == 6 && r.Depth == 1).HeroLevel, "all'Abisso IV si è al massimo");
        }

        [Test, Description("La rarità dell'equipaggiamento segue la difficoltà: normale al Crepuscolo, magica all'Ombra e alla Tenebra, rara dall'Abisso")]
        public void Gear_FollowsTheDifficulty()
        {
            Assert.AreEqual(Items.Rarity.Normal, DifficultyCensus.GearFor(0));
            Assert.AreEqual(Items.Rarity.Magic, DifficultyCensus.GearFor(1));
            Assert.AreEqual(Items.Rarity.Magic, DifficultyCensus.GearFor(2));
            Assert.AreEqual(Items.Rarity.Rare, DifficultyCensus.GearFor(3));
            Assert.AreEqual(Items.Rarity.Rare, DifficultyCensus.GearFor(8));
        }
    }
}

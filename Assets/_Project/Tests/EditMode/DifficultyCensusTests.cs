using System.Collections.Generic;
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
            MapChecks.Pass("misura delle difficoltà", CensusChecks.EveryDepth(_rows));
        }

        [Test, Description("Dall'Ombra ogni discesa comincia dura (bruto in 9–14 colpi alla profondità 1), non più facile della precedente oltre il 5%")]
        public void EachDifficulty_StartsHarder()
        {
            MapChecks.Pass("misura delle difficoltà", CensusChecks.EachDifficultyStartsHarder(_rows, _table.Count));
        }

        [Test, Description("Il re dura un po' di più a ogni gradino: non meno del 95% dei colpi del precedente, e meno di una volta e mezza")]
        public void King_LastsALittleLonger()
        {
            MapChecks.Pass("misura delle difficoltà", CensusChecks.KingLastsALittleLonger(_rows));
        }

        [Test, Description("Con una discesa per difficoltà il cavaliere comincia ognuna al più due livelli sotto la zona, e arriva al 50 prima dell'Abisso IV")]
        public void Experience_KeepsUpWithTheZone()
        {
            MapChecks.Pass("misura delle difficoltà", CensusChecks.ExperienceKeepsUp(_rows, _table, _census.Settings));
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

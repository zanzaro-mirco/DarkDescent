using System;
using DarkDescent.Combat;
using DarkDescent.Core;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class CombatFormulasTests
    {
        [Test, Description("Probabilità di colpire: 75 + Destrezza / 2 − Armatura")]
        public void HitChance_FollowsFormula()
        {
            // il cavaliere contro lo scheletro, e lo scheletro contro il cavaliere senza e con lo scudo
            Assert.AreEqual(75f, CombatFormulas.HitChance(20f, 10f));
            Assert.AreEqual(80f, CombatFormulas.HitChance(10f, 0f));
            Assert.AreEqual(75f, CombatFormulas.HitChance(10f, 5f));
        }

        [Test, Description("La probabilità di colpire resta tra 5 e 95, comunque siano Destrezza e Armatura")]
        public void HitChance_IsClamped()
        {
            Assert.AreEqual(95f, CombatFormulas.HitChance(200f, 0f));
            Assert.AreEqual(5f, CombatFormulas.HitChance(0f, 500f));
        }

        [Test, Description("Il tiro per colpire confronta il numero casuale con la probabilità")]
        public void RollHit_ComparesWithChance()
        {
            Assert.IsTrue(CombatFormulas.RollHit(75f, new FixedRandomSource(0.7499)));
            Assert.IsFalse(CombatFormulas.RollHit(75f, new FixedRandomSource(0.75)));
            Assert.IsTrue(CombatFormulas.RollHit(5f, new FixedRandomSource(0.0)), "con il 5% qualcosa va sempre a segno");
            Assert.IsFalse(CombatFormulas.RollHit(95f, new FixedRandomSource(0.99)), "con il 95% qualcosa va sempre a vuoto");
        }

        [Test, Description("Il danno è un intero tra minimo e massimo dell'arma, estremi compresi, per 1 + Forza / 100, arrotondato")]
        public void RollDamage_CoversRangeTimesStrength()
        {
            Assert.AreEqual(8f, CombatFormulas.RollDamage(6, 9, 30f, new FixedRandomSource(0.0)), "6 × 1,3 = 7,8");
            Assert.AreEqual(12f, CombatFormulas.RollDamage(6, 9, 30f, new FixedRandomSource(0.9999999)), "9 × 1,3 = 11,7");
            Assert.AreEqual(7f, CombatFormulas.RollDamage(6, 9, 0f, new FixedRandomSource(0.25)));
        }

        [Test, Description("Con la Forza il danno resta intero e almeno 1: la vita non resta mai frazionaria, e l'ultimo colpo non mostra 0")]
        public void RollDamage_IsAlwaysWholeAndPositive()
        {
            var random = new SystemRandomSource(4711);
            for (int i = 0; i < 2000; i++)
            {
                float damage = CombatFormulas.RollDamage(8, 12, 25f, random);
                Assert.AreEqual(Math.Floor(damage), damage, "intero anche con la Forza 25");
            }

            Assert.AreEqual(1f, CombatFormulas.RollDamage(0, 0, 0f, new FixedRandomSource(0.0)), "almeno 1");
        }

        [Test, Description("Su molti tiri il danno non esce mai dall'intervallo e prende tutti i valori")]
        public void RollDamage_StaysInRange()
        {
            var random = new SystemRandomSource(4711);
            var seen = new bool[4];
            for (int i = 0; i < 2000; i++)
            {
                float damage = CombatFormulas.RollDamage(6, 9, 0f, random);
                Assert.That(damage, Is.InRange(6f, 9f));
                Assert.AreEqual(Math.Floor(damage), damage, "senza Forza il tiro è intero");
                seen[(int)damage - 6] = true;
            }

            CollectionAssert.DoesNotContain(seen, false, "ogni valore tra 6 e 9 deve uscire");
        }

        [Test, Description("Vita massima: 50 + 2 × Vitalità; con la Vitalità del cavaliere fa 100, la vita della M2")]
        public void MaxLife_FromVitality()
        {
            Assert.AreEqual(100f, CombatFormulas.MaxLife(25f));
            Assert.AreEqual(50f, CombatFormulas.MaxLife(0f));
        }

        [Test, Description("Il bonus a colpire degli affissi si somma, sempre dentro 5–95; le percentuali degli oggetti arrotondano per difetto")]
        public void ToHitBonus_AndItemPercent()
        {
            Assert.AreEqual(80f, CombatFormulas.HitChance(20f, 10f, 5f), "75 + 10 − 10 + 5");
            Assert.AreEqual(95f, CombatFormulas.HitChance(20f, 0f, 40f));
            Assert.AreEqual(8, CombatFormulas.ApplyPercent(6, 40));
            Assert.AreEqual(12, CombatFormulas.ApplyPercent(9, 40));
            Assert.AreEqual(7, CombatFormulas.ApplyPercent(5, 50));
            Assert.AreEqual(9, CombatFormulas.ApplyPercent(9, 0));
        }

        [Test, Description("Blocco: scudo + Destrezza / 2, tra 0 e 75; il tiro blocca sotto la probabilità")]
        public void BlockChance_ClampedTo75()
        {
            Assert.AreEqual(20f, CombatFormulas.BlockChance(10f, 20f), "scudo con stemma e cavaliere");
            Assert.AreEqual(75f, CombatFormulas.BlockChance(60f, 40f));
            Assert.AreEqual(0f, CombatFormulas.BlockChance(0f, 0f));

            Assert.IsTrue(CombatFormulas.RollBlock(20f, new FixedRandomSource(0.19)));
            Assert.IsFalse(CombatFormulas.RollBlock(20f, new FixedRandomSource(0.2)));
            Assert.IsFalse(CombatFormulas.RollBlock(0f, new FixedRandomSource(0.0)), "senza probabilità mai");
        }

        [Test, Description("Critico: 5% + Destrezza / 10, al massimo 50% (D13 della M7); 7% con la Destrezza 20 del cavaliere")]
        public void CritChance_FromDexterity()
        {
            Assert.AreEqual(5f, CombatFormulas.CritChance(0f));
            Assert.AreEqual(7f, CombatFormulas.CritChance(20f), 0.0001f);
            Assert.AreEqual(50f, CombatFormulas.CritChance(450f));
            Assert.AreEqual(50f, CombatFormulas.CritChance(1000f), "il tetto");
            Assert.AreEqual(2f, CombatFormulas.CritMultiplier, "danno doppio, come il guerriero di Diablo 1");
        }

        [Test, Description("Il critico esce dalla parte alta del tiro: con i tiri fissi a 0 dei test non c'è mai (trappola 9)")]
        public void RollCrit_UsesTheTopOfTheRoll()
        {
            Assert.IsFalse(CombatFormulas.RollCrit(7f, new FixedRandomSource(0.0)), "il tiro dei test");
            Assert.IsFalse(CombatFormulas.RollCrit(7f, new FixedRandomSource(0.9299)));
            Assert.IsTrue(CombatFormulas.RollCrit(7f, new FixedRandomSource(0.93)));
            Assert.IsTrue(CombatFormulas.RollCrit(7f, new FixedRandomSource(0.9999)));
            Assert.IsFalse(CombatFormulas.RollCrit(0f, new FixedRandomSource(0.9999)), "senza probabilità mai");
            Assert.IsTrue(CombatFormulas.RollCrit(50f, new FixedRandomSource(0.5)));
        }
    }
}

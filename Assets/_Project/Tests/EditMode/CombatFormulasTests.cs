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

        [Test, Description("Il danno è un intero tra minimo e massimo dell'arma, estremi compresi, per 1 + Forza / 100")]
        public void RollDamage_CoversRangeTimesStrength()
        {
            Assert.AreEqual(6f * 1.3f, CombatFormulas.RollDamage(6, 9, 30f, new FixedRandomSource(0.0)), 0.0001f);
            Assert.AreEqual(9f * 1.3f, CombatFormulas.RollDamage(6, 9, 30f, new FixedRandomSource(0.9999999)), 0.0001f);
            Assert.AreEqual(7f, CombatFormulas.RollDamage(6, 9, 0f, new FixedRandomSource(0.25)), 0.0001f);
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
    }
}

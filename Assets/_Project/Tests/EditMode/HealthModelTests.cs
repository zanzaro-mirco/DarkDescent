using System;
using DarkDescent.Combat;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class HealthModelTests
    {
        [Test, Description("Il danno riduce la vita e restituisce quanto applicato")]
        public void ApplyDamage_ReducesCurrent()
        {
            var health = new HealthModel(100f);

            float applied = health.ApplyDamage(30f);

            Assert.AreEqual(30f, applied);
            Assert.AreEqual(70f, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [Test, Description("La vita non scende sotto zero: un colpo eccessivo applica solo la vita rimasta")]
        public void ApplyDamage_NeverGoesBelowZero()
        {
            var health = new HealthModel(50f);

            float applied = health.ApplyDamage(80f);

            Assert.AreEqual(50f, applied);
            Assert.AreEqual(0f, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [Test, Description("A zero Died scatta una volta sola, anche con colpi successivi")]
        public void Died_IsRaisedOnlyOnce()
        {
            var health = new HealthModel(10f);
            int deaths = 0;
            health.Died += () => deaths++;

            health.ApplyDamage(10f);
            health.ApplyDamage(10f);
            health.ApplyDamage(5f);

            Assert.AreEqual(1, deaths);
        }

        [Test, Description("Dopo la morte il danno vale 0 e non emette Changed")]
        public void AfterDeath_DamageIsIgnored()
        {
            var health = new HealthModel(10f);
            health.ApplyDamage(10f);
            int changes = 0;
            health.Changed += (_, _) => changes++;

            float applied = health.ApplyDamage(5f);

            Assert.AreEqual(0f, applied);
            Assert.AreEqual(0, changes);
            Assert.AreEqual(0f, health.Current);
        }

        [TestCase(0f)]
        [TestCase(-15f)]
        [TestCase(float.NaN)]
        [Description("Un danno nullo, negativo o NaN non cura e non emette eventi")]
        public void NonPositiveDamage_IsIgnored(float amount)
        {
            var health = new HealthModel(40f);
            health.ApplyDamage(10f);
            int changes = 0;
            health.Changed += (_, _) => changes++;

            float applied = health.ApplyDamage(amount);

            Assert.AreEqual(0f, applied);
            Assert.AreEqual(30f, health.Current);
            Assert.AreEqual(0, changes);
        }

        [Test, Description("Changed riporta vita corrente e massima")]
        public void Changed_ReportsCurrentAndMax()
        {
            var health = new HealthModel(80f);
            float reportedCurrent = -1f, reportedMax = -1f;
            health.Changed += (current, max) => { reportedCurrent = current; reportedMax = max; };

            health.ApplyDamage(20f);

            Assert.AreEqual(60f, reportedCurrent);
            Assert.AreEqual(80f, reportedMax);
        }

        [Test, Description("Vita massima che sale: l'attuale sale della stessa quantità e lo dice")]
        public void SetMax_Up_RaisesCurrentBySameAmount()
        {
            var model = new HealthModel(100f);
            model.ApplyDamage(30f);
            float reportedMax = 0f;
            model.Changed += (current, max) => reportedMax = max;

            model.SetMax(120f);

            Assert.AreEqual(90f, model.Current);
            Assert.AreEqual(120f, model.Max);
            Assert.AreEqual(120f, reportedMax);
        }

        [Test, Description("Vita massima che scende: l'attuale scende della stessa quantità ma mai sotto 1, e non resuscita")]
        public void SetMax_Down_NeverKills()
        {
            var model = new HealthModel(120f);
            model.SetMax(100f);
            Assert.AreEqual(100f, model.Current);

            model.ApplyDamage(95f);
            model.SetMax(80f);
            Assert.AreEqual(1f, model.Current, "togliere un oggetto non uccide");
            Assert.IsFalse(model.IsDead);

            model.ApplyDamage(10f);
            model.SetMax(150f);
            Assert.IsTrue(model.IsDead);
            Assert.AreEqual(0f, model.Current, "da morto cambia solo il massimo");
            Assert.Throws<ArgumentOutOfRangeException>(() => model.SetMax(0f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [Description("La vita massima deve essere positiva")]
        public void Constructor_RejectsNonPositiveMax(float max)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HealthModel(max));
        }
    }
}

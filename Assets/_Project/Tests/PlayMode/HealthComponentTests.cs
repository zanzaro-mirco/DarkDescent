using System.Collections.Generic;
using DarkDescent.Combat;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class HealthComponentTests
    {
        private GameObject _go;
        private Health _health;
        private readonly List<string> _events = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Target");
            _health = _go.AddComponent<Health>();
            _events.Clear();
            _health.HealthChanged += (current, max) => _events.Add($"changed {current}/{max}");
            _health.Damaged += (info, applied) => _events.Add($"damaged {applied}");
            _health.Died += () => _events.Add("died");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
        }

        [Test, Description("Un colpo emette vita cambiata e colpo ricevuto, in quest'ordine")]
        public void TakeDamage_RaisesChangedThenDamaged()
        {
            _health.TakeDamage(new DamageInfo(30f, DamageType.Physical, null));

            CollectionAssert.AreEqual(new[] { "changed 70/100", "damaged 30" }, _events);
            Assert.AreEqual(70f, _health.Current);
        }

        [Test, Description("Il colpo letale emette Died una volta sola, dopo vita e colpo; i colpi successivi sono ignorati")]
        public void LethalDamage_RaisesDiedOnceAndLast()
        {
            _health.TakeDamage(new DamageInfo(150f, DamageType.Physical, null));
            _health.TakeDamage(new DamageInfo(10f, DamageType.Physical, null));

            CollectionAssert.AreEqual(new[] { "changed 0/100", "damaged 100", "died" }, _events);
            Assert.IsTrue(_health.IsDead);
        }

        [Test, Description("Un danno nullo non emette niente")]
        public void ZeroDamage_RaisesNothing()
        {
            _health.TakeDamage(new DamageInfo(0f, DamageType.Physical, null));

            Assert.IsEmpty(_events);
        }

        [Test, Description("Health si usa anche attraverso IDamageable")]
        public void Health_IsReachableAsDamageable()
        {
            IDamageable damageable = _go.GetComponent<IDamageable>();
            damageable.TakeDamage(new DamageInfo(100f, DamageType.Physical, null));

            Assert.IsTrue(damageable.IsDead);
        }
    }
}

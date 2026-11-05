using System.Collections.Generic;
using DarkDescent.UI;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>Le regole delle barre sopra i nemici (D9 e D10 della M23): chi ne ha una, quanto è piena, quali restano.</summary>
    public class EnemyBarRulesTests
    {
        [TestCase(100f, 100f, false, TestName = "Intact_HasNoBar")]
        [TestCase(99f, 100f, true, TestName = "AScratch_HasABar")]
        [TestCase(1f, 100f, true, TestName = "AlmostDead_HasABar")]
        [TestCase(0f, 100f, false, TestName = "Dead_HasNoBar")]
        [TestCase(-5f, 100f, false, TestName = "BelowZero_HasNoBar")]
        [TestCase(0f, 0f, false, TestName = "NoMaximum_HasNoBar")]
        [Description("Una barra spetta a chi è vivo e ha perso della vita: intatto o morto no")]
        public void IsWounded_OnlyWhenAliveAndHurt(float current, float max, bool expected)
        {
            Assert.AreEqual(expected, EnemyBarRules.IsWounded(current, max));
        }

        [Test, Description("Il riempimento è la vita su vita massima, tenuto tra 0 e 1")]
        public void Fill_IsTheShareOfLife()
        {
            Assert.AreEqual(0.62f, EnemyBarRules.Fill(62f, 100f), 0.0001f);
            Assert.AreEqual(1f, EnemyBarRules.Fill(120f, 100f));
            Assert.AreEqual(0f, EnemyBarRules.Fill(-3f, 100f));
            Assert.AreEqual(0f, EnemyBarRules.Fill(10f, 0f));
        }

        [Test, Description("Il tetto è 24, come nel disegno")]
        public void MaxBars_IsTwentyFour()
        {
            Assert.AreEqual(24, EnemyBarRules.MaxBars);
        }

        [Test, Description("Con più candidati del tetto restano i più vicini, dal più vicino al più lontano")]
        public void PickNearest_KeepsTheClosestInOrder()
        {
            var distances = new List<float> { 9f, 1f, 16f, 4f, 25f, 0.25f };
            var picked = new List<int>();
            EnemyBarRules.PickNearest(distances, 3, picked);
            CollectionAssert.AreEqual(new[] { 5, 1, 3 }, picked);
        }

        [Test, Description("Con meno candidati del tetto restano tutti")]
        public void PickNearest_BelowTheLimit_KeepsEveryone()
        {
            var picked = new List<int>();
            EnemyBarRules.PickNearest(new List<float> { 5f, 2f }, 24, picked);
            CollectionAssert.AreEqual(new[] { 1, 0 }, picked);

            EnemyBarRules.PickNearest(new List<float>(), 24, picked);
            Assert.IsEmpty(picked, "nessuno, nessuna barra");
        }

        [Test, Description("Trenta feriti danno ventiquattro barre, e sono i ventiquattro più vicini")]
        public void PickNearest_ThirtyCandidates_CapsAtTwentyFour()
        {
            var distances = new List<float>();
            for (int i = 0; i < 30; i++)
            {
                // dal più lontano al più vicino, perché l'ordine della lista non conti
                distances.Add((30 - i) * (30 - i));
            }

            var picked = new List<int>();
            EnemyBarRules.PickNearest(distances, EnemyBarRules.MaxBars, picked);

            Assert.AreEqual(24, picked.Count);
            Assert.AreEqual(29, picked[0], "il più vicino è l'ultimo della lista");
            Assert.AreEqual(6, picked[23], "il ventiquattresimo è il settimo da fondo");
            CollectionAssert.DoesNotContain(picked, 0, "il più lontano resta senza");
            CollectionAssert.DoesNotContain(picked, 5);
        }

        [Test, Description("A pari distanza vince chi sta prima nella lista; un tetto di zero non sceglie nessuno")]
        public void PickNearest_TiesAndZero()
        {
            var picked = new List<int>();
            EnemyBarRules.PickNearest(new List<float> { 4f, 4f, 4f }, 2, picked);
            CollectionAssert.AreEqual(new[] { 0, 1 }, picked);

            EnemyBarRules.PickNearest(new List<float> { 1f }, 0, picked);
            Assert.IsEmpty(picked);
        }

        [Test, Description("La lista passata si svuota prima: due chiamate di fila non si sommano")]
        public void PickNearest_ClearsTheListFirst()
        {
            var picked = new List<int> { 99, 98 };
            EnemyBarRules.PickNearest(new List<float> { 3f }, 5, picked);
            CollectionAssert.AreEqual(new[] { 0 }, picked);
        }
    }
}

using System.Collections.Generic;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La statua che si anima (25.5 della M25, D7 e D16): quando un livello ne ha una, quale cassa guarda, e il tetto di tre segreti.</summary>
    public class StatueRulesTests
    {
        private const int Seeds = 2000;

        [Test, Description("Un livello su due della cripta, dalla profondità 2 alla 4, vuole una statua; fuori da lì mai (D7, D16)")]
        public void OneLevelInTwo_InTheCrypt()
        {
            for (int depth = StatueRules.FirstDepth; depth <= StatueRules.LastDepth; depth++)
            {
                int with = 0;
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    with += StatueRules.LevelWantsOne(seed, depth) ? 1 : 0;
                }

                Assert.That(with, Is.InRange(Seeds * 0.45, Seeds * 0.55), $"profondità {depth}: {with} su {Seeds}");
            }

            for (ulong seed = 1; seed <= 200; seed++)
            {
                foreach (int depth in new[] { 0, 1, 5, 6, 7, 8 })
                {
                    Assert.IsFalse(StatueRules.LevelWantsOne(seed, depth), $"profondità {depth}, seme {seed}");
                }
            }
        }

        [Test, Description("La cassa della statua dipende solo dalle posizioni e dal seme, non dall'ordine né da quante sono aperte (ADR-079)")]
        public void ThePick_DependsOnlyOnTheCellsAndTheSeed()
        {
            var cells = new List<Vector2Int> { new Vector2Int(120, 40), new Vector2Int(380, 210), new Vector2Int(90, 500), new Vector2Int(640, 330) };
            for (ulong seed = 1; seed <= 200; seed++)
            {
                int pick = StatueRules.Pick(seed, 3, cells);
                Assert.That(pick, Is.InRange(0, cells.Count - 1));
                var reversed = new List<Vector2Int>(cells);
                reversed.Reverse();
                Assert.AreEqual(cells[pick], reversed[StatueRules.Pick(seed, 3, reversed)], $"seme {seed}: l'ordine non conta");
            }

            Assert.AreEqual(-1, StatueRules.Pick(1UL, 3, new List<Vector2Int>()), "senza casse non c'è");
        }

        [Test, Description("Ogni cassa viene scelta, a seconda del seme: la statua non sta sempre alla stessa")]
        public void EveryChestCanBePicked()
        {
            var cells = new List<Vector2Int> { new Vector2Int(120, 40), new Vector2Int(380, 210), new Vector2Int(90, 500) };
            var picked = new HashSet<int>();
            for (ulong seed = 1; seed <= 200; seed++)
            {
                picked.Add(StatueRules.Pick(seed, 2, cells));
            }

            Assert.AreEqual(3, picked.Count);
        }

        [Test, Description("Il tetto di tre segreti (D16): con stanza sigillata, prigioniero e baule maledetto già presenti la statua cede; con meno, resta")]
        public void TheCapOfThree_MakesTheStatueYield()
        {
            Assert.IsTrue(StatueRules.Yields(true, true, true));
            Assert.IsFalse(StatueRules.Yields(true, true, false));
            Assert.IsFalse(StatueRules.Yields(true, false, true));
            Assert.IsFalse(StatueRules.Yields(false, true, true));
            Assert.IsFalse(StatueRules.Yields(false, false, false));
        }
    }
}

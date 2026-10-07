using System;
using System.Collections.Generic;
using DarkDescent.Combat;
using DarkDescent.Core;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class SwingPatternTests
    {
        [Test, Description("Il primo colpo è forte; poi tanti leggeri quanti dice il tiro, poi di nuovo forte")]
        public void HeavyFirst_ThenRolledQuickOnes()
        {
            var pattern = new SwingPattern(1, 3);

            // tiro basso: un leggero; tiro alto: tre
            var random = new FixedRandomSource(0.0, 0.99);
            var swings = new List<bool>();
            for (int i = 0; i < 7; i++)
            {
                swings.Add(pattern.NextIsHeavy(random));
            }

            CollectionAssert.AreEqual(new[] { true, false, true, false, false, false, true }, swings);
        }

        [Test, Description("Su 10.000 colpi a caso i forti sono uno ogni tre (uno forte e in media due leggeri), mai due di fila")]
        public void OneHeavyEveryThree_NeverTwoInARow()
        {
            var pattern = new SwingPattern(1, 3);
            var random = new SplitMix64Source(4711UL);
            int heavy = 0;
            bool previous = false;
            for (int i = 0; i < 10000; i++)
            {
                bool now = pattern.NextIsHeavy(random);
                Assert.IsFalse(previous && now, $"due colpi forti di fila al colpo {i}");
                heavy += now ? 1 : 0;
                previous = now;
            }

            Assert.AreEqual(1.0 / 3.0, heavy / 10000.0, 0.01);
        }

        [Test, Description("Intervalli impossibili rifiutati")]
        public void InvalidRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SwingPattern(3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SwingPattern(-1, 2));
        }
    }
}

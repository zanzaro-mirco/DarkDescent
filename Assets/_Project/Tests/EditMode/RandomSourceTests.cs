using System;
using DarkDescent.Core;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class RandomSourceTests
    {
        [Test, Description("Stesso seme, stessa sequenza: è la base dei drop riproducibili della M5")]
        public void SystemRandomSource_SameSeedSameSequence()
        {
            var a = new SystemRandomSource(4711);
            var b = new SystemRandomSource(4711);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.NextDouble(), b.NextDouble());
            }
        }

        [Test, Description("La sorgente fissa restituisce i valori dati, in ciclo")]
        public void FixedRandomSource_CyclesValues()
        {
            var random = new FixedRandomSource(0.1, 0.5);

            Assert.AreEqual(0.1, random.NextDouble());
            Assert.AreEqual(0.5, random.NextDouble());
            Assert.AreEqual(0.1, random.NextDouble());
        }

        [Test, Description("La sorgente fissa rifiuta valori fuori da [0, 1)")]
        public void FixedRandomSource_RejectsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedRandomSource(1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedRandomSource(-0.1));
            Assert.Throws<ArgumentException>(() => new FixedRandomSource());
        }
    }
}

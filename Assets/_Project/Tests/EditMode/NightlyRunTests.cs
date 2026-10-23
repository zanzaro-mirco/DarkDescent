using System;
using System.Collections.Generic;
using DarkDescent.Core;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>Il seme della ronda notturna (D4 della M15): da dove viene e come se ne ricavano i semi dei casi.</summary>
    public class NightlyRunTests
    {
        private static readonly DateTime Night = new DateTime(2026, 10, 24, 1, 0, 0, DateTimeKind.Utc);

        [Test, Description("Il seme viene dalla riga di comando, poi dalla variabile, poi dalla data: 24 ottobre 2026 è 20261024")]
        public void Seed_ComesFromArgumentsVariableOrDate()
        {
            var args = new[] { "Unity.exe", "-batchmode", NightlyRun.SeedOption, "4711" };
            Assert.AreEqual(4711UL, NightlyRun.Seed(args, "99", Night));
            Assert.AreEqual(99UL, NightlyRun.Seed(new[] { "Unity.exe" }, " 99 ", Night));
            Assert.AreEqual(20261024UL, NightlyRun.Seed(new[] { "Unity.exe" }, null, Night));
        }

        [Test, Description("Un seme che non è un numero si salta: si passa al successivo")]
        public void Seed_SkipsWhatIsNotANumber()
        {
            var args = new[] { NightlyRun.SeedOption, "domani" };
            Assert.AreEqual(99UL, NightlyRun.Seed(args, "99", Night));
            Assert.AreEqual(20261024UL, NightlyRun.Seed(args, "-3", Night));
        }

        [Test, Description("Ogni caso di ogni parte ha un seme suo, sempre lo stesso nella stessa notte, e la notte dopo cambiano tutti")]
        public void ForNightly_GivesEveryCaseItsSeed()
        {
            var seen = new HashSet<ulong>();
            for (int part = 0; part < 4; part++)
            {
                for (int index = 0; index < 100; index++)
                {
                    ulong seed = SeedMixer.ForNightly(20261024UL, part, index);
                    Assert.IsTrue(seen.Add(seed), $"parte {part}, caso {index}: seme già visto");
                    Assert.AreEqual(seed, SeedMixer.ForNightly(20261024UL, part, index));
                    Assert.AreNotEqual(seed, SeedMixer.ForNightly(20261025UL, part, index));
                }
            }
        }
    }
}

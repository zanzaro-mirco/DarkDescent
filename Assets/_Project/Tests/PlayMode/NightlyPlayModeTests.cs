using DarkDescent.Core;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La ronda notturna in PlayMode (M15): gira solo di notte, con la categoria <see cref="NightlyRun.Category"/>.</summary>
    [Category(NightlyRun.Category)]
    public class NightlyPlayModeTests
    {
        [Test, Description("La ronda sa il seme della notte anche in PlayMode, dove arriva dalla stessa riga di comando")]
        public void Round_KnowsTheSeed()
        {
            ulong seed = NightlyRun.Seed();
            Assert.Greater(seed, 0UL);
            Debug.Log($"[DarkDescent] ronda notturna, PlayMode: seme della notte {seed} (per rifarla: {NightlyRun.SeedOption} {seed})");
        }
    }
}

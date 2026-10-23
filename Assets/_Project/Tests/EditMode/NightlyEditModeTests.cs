using DarkDescent.Core;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>La ronda notturna in EditMode (M15): gira solo di notte, con la categoria <see cref="NightlyRun.Category"/>.</summary>
    [Category(NightlyRun.Category)]
    public class NightlyEditModeTests
    {
        [Test, Description("La ronda sa il seme della notte e lo scrive nel log, per rifarla")]
        public void Round_KnowsTheSeed()
        {
            ulong seed = NightlyRun.Seed();
            Assert.Greater(seed, 0UL);
            Debug.Log($"[DarkDescent] ronda notturna, EditMode: seme della notte {seed} (per rifarla: {NightlyRun.SeedOption} {seed})");
        }
    }
}

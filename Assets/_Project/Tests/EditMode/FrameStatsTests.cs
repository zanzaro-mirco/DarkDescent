using DarkDescent.Diagnostics;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarkDescent.Tests
{
    /// <summary>I conti della misura delle prestazioni in build (D6 della M11).</summary>
    public class FrameStatsTests
    {
        [Test, Description("Media, percentili al rango più vicino, massimo e secondi misurati")]
        public void Frames_AverageAndPercentiles()
        {
            var stats = new FrameStats(200);
            for (int ms = 100; ms >= 1; ms--)
            {
                stats.Add(ms, ms / 2f, 0, 10, 5, 1000);
            }

            Assert.AreEqual(100, stats.Count);
            Assert.AreEqual(50.5d, stats.FrameAverage, 1e-9);
            Assert.AreEqual(95f, stats.FramePercentile(95f));
            Assert.AreEqual(99f, stats.FramePercentile(99f));
            Assert.AreEqual(100f, stats.FrameMax);
            Assert.AreEqual(5.05d, stats.Seconds, 1e-9);
            Assert.AreEqual(49.5f, stats.CpuPercentile(99f));
            Assert.AreEqual(10d, stats.BatchesAverage);
        }

        [Test, Description("Le allocazioni: quanti frame allocano, media e massimo")]
        public void Alloc_CountsTheFrames()
        {
            var stats = new FrameStats(10);
            foreach (long bytes in new long[] { 0, 0, 100, 0, 300 })
            {
                stats.Add(16f, -1f, bytes, -1, -1, -1);
            }

            Assert.IsTrue(stats.HasAlloc);
            Assert.AreEqual(2, stats.AllocatingFrames);
            Assert.AreEqual(300, stats.AllocMax);
            Assert.AreEqual(80d, stats.AllocAverage);
            StringAssert.Contains("allocazioni in 2 frame, media 80 B, max 300 B", stats.Describe("prova"));
        }

        [Test, Description("Senza i contatori di una build di sviluppo la riga lo dice, e Reset riparte da zero")]
        public void MissingCounters_AndReset()
        {
            var stats = new FrameStats(10);
            stats.Add(10f, -1f, -1, -1, -1, -1);
            stats.Add(20f, -1f, -1, -1, -1, -1);
            Assert.IsFalse(stats.HasCpu);
            Assert.IsFalse(stats.HasRender);
            string line = stats.Describe("cripta");
            StringAssert.StartsWith("cripta: 2 frame in 0.0 s, media 15.00 ms (67 fps)", line);
            StringAssert.Contains("allocazioni n/d", line);
            StringAssert.DoesNotContain("main thread", line);

            stats.Reset();
            Assert.AreEqual(0, stats.Count);
            Assert.AreEqual(0d, stats.Seconds);
            Assert.AreEqual(0f, stats.FrameMax);
        }

        [Test, Description("Oltre la capacità i frame contano nei secondi ma non nelle durate")]
        public void BeyondCapacity_OnlySeconds()
        {
            var stats = new FrameStats(2);
            stats.Add(10f, -1f, -1, -1, -1, -1);
            stats.Add(10f, -1f, -1, -1, -1, -1);
            stats.Add(1000f, -1f, -1, -1, -1, -1);
            Assert.AreEqual(2, stats.Count);
            Assert.AreEqual(10f, stats.FrameMax);
            Assert.AreEqual(1.02d, stats.Seconds, 1e-9);
        }

        [Test, Description("Aggiungere un frame non alloca: la misura non sporca quello che misura")]
        public void Add_DoesNotAllocate()
        {
            var stats = new FrameStats(1000);
            stats.Add(16f, 8f, 0, 100, 40, 50000);
            Assert.That(() => stats.Add(16f, 8f, 0, 100, 40, 50000), Is.Not.AllocatingGCMemory());
        }
    }
}

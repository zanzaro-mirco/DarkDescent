using System.Collections.Generic;
using DarkDescent.Audio;
using DarkDescent.Core;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class StingerScheduleTests
    {
        private const int Draws = 2000;

        private static StingerSchedule Schedule(int seed = 7)
        {
            return new StingerSchedule(new SystemRandomSource(seed));
        }

        [Test, Description("L'attesa tra due versi e l'intonazione stanno sempre negli intervalli del profilo, e li coprono")]
        public void DelayAndPitch_StayInTheirRanges()
        {
            var schedule = Schedule();
            var interval = new Vector2(20f, 50f);
            float least = float.MaxValue, most = float.MinValue;
            for (int i = 0; i < Draws; i++)
            {
                float delay = schedule.NextDelay(interval);
                Assert.That(delay, Is.InRange(20f, 50f));
                least = Mathf.Min(least, delay);
                most = Mathf.Max(most, delay);
                Assert.That(schedule.NextPitch(new Vector2(0.75f, 1.05f)), Is.InRange(0.75f, 1.05f));
            }

            Assert.Less(least, 22f, "anche le attese brevi");
            Assert.Greater(most, 48f, "anche quelle lunghe");
        }

        [Test, Description("Il verso nasce in piano attorno al cavaliere, alla distanza chiesta, da tutte le direzioni")]
        public void Offset_IsFlatAtTheRightDistanceAllAround()
        {
            var schedule = Schedule();
            var quadrants = new int[4];
            for (int i = 0; i < Draws; i++)
            {
                Vector3 offset = schedule.NextOffset(new Vector2(10f, 18f));
                Assert.AreEqual(0f, offset.y, "in piano");
                Assert.That(offset.magnitude, Is.InRange(10f - 1e-4f, 18f + 1e-4f));
                quadrants[(offset.x >= 0f ? 0 : 1) + (offset.z >= 0f ? 0 : 2)]++;
            }

            foreach (int count in quadrants)
            {
                Assert.Greater(count, Draws / 8, "ogni quarto attorno al cavaliere");
            }
        }

        [Test, Description("Mai lo stesso verso due volte di fila, e prima o poi tutti")]
        public void Index_NeverRepeatsAndCoversAll()
        {
            var schedule = Schedule();
            var seen = new HashSet<int>();
            int previous = -1;
            for (int i = 0; i < Draws; i++)
            {
                int index = schedule.NextIndex(8);
                Assert.That(index, Is.InRange(0, 7));
                Assert.AreNotEqual(previous, index, $"al tiro {i}");
                seen.Add(index);
                previous = index;
            }

            Assert.AreEqual(8, seen.Count);
        }

        [Test, Description("Ai bordi: un verso solo si ripete per forza; il tiro massimo resta nell'intervallo; un profilo con meno versi non esce dal suo")]
        public void Index_Edges()
        {
            var single = Schedule();
            Assert.AreEqual(0, single.NextIndex(1));
            Assert.AreEqual(0, single.NextIndex(1));

            // 0,999...: l'ultimo degli altri, saltando il precedente
            var high = new StingerSchedule(new FixedRandomSource(0.0, 0.9999));
            Assert.AreEqual(0, high.NextIndex(3));
            Assert.AreEqual(2, high.NextIndex(3));

            var shrinking = new StingerSchedule(new FixedRandomSource(0.9999));
            Assert.AreEqual(7, shrinking.NextIndex(8));
            Assert.That(shrinking.NextIndex(3), Is.InRange(0, 2), "dopo l'ultimo di otto, uno di tre");
        }
    }
}

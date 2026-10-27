using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>Il giorno che passa nel borgo (D5 della M18): il momento dal tempo di gioco.</summary>
    public class DayCycleTests
    {
        private const string LightingPath = "Assets/_Project/Data/Levels/VillageDayLighting.asset";

        [Test, Description("Stesso tempo, stesso momento; un giro intero dopo, ancora lo stesso")]
        public void SameTime_SameMoment()
        {
            foreach (float time in new[] { 0f, 100f, 530f, 905f, 1199f })
            {
                var a = DayCycle.At(time);
                var b = DayCycle.At(time + DayCycle.CycleSeconds);
                Assert.AreEqual((a.From, a.To), (b.From, b.To), $"{time}");
                Assert.AreEqual(a.Blend, b.Blend, 0.001f, $"{time}");
            }
        }

        [Test, Description("La partita nuova comincia di giorno; poi tramonto, notte, alba e di nuovo giorno, in quest'ordine")]
        public void Moments_ComeInOrder()
        {
            Assert.AreEqual(DayMoment.Day, DayCycle.At(0f).From);
            var seen = new System.Collections.Generic.List<DayMoment>();
            for (float time = 0f; time < DayCycle.CycleSeconds * 1.2f; time += 5f)
            {
                var moment = DayCycle.At(time).From;
                if (seen.Count == 0 || seen[seen.Count - 1] != moment)
                {
                    seen.Add(moment);
                }
            }

            CollectionAssert.AreEqual(new[] { DayMoment.Day, DayMoment.Dusk, DayMoment.Night, DayMoment.Dawn, DayMoment.Day }, seen);
            foreach (DayMoment moment in System.Enum.GetValues(typeof(DayMoment)))
            {
                var start = DayCycle.At(DayCycle.Start(moment) + 1f);
                Assert.AreEqual((moment, 0f), (start.From, start.Blend), $"{moment} pieno al suo inizio");
            }
        }

        [Test, Description("Il passaggio da un momento all'altro dura un minuto e non salta: la miscela cresce a piccoli passi")]
        public void Blend_IsContinuous()
        {
            float previous = Weight(0f);
            for (float time = 0.5f; time < DayCycle.CycleSeconds * 2f; time += 0.5f)
            {
                float weight = Weight(time);
                // in cerchio: dopo la notte (3) viene l'alba (0)
                Assert.Less(Mathf.Abs(Mathf.Repeat(weight - previous + 2f, 4f) - 2f), 0.02f, $"salto a {time} s");
                previous = weight;
            }

            var middle = DayCycle.At(DayCycle.Start(DayMoment.Dusk) - DayCycle.TransitionSeconds * 0.5f);
            Assert.AreEqual((DayMoment.Day, DayMoment.Dusk), (middle.From, middle.To));
            Assert.AreEqual(0.5f, middle.Blend, 0.01f);
        }

        [Test, Description("Lanterne e finestre spente di giorno, accese dal tramonto all'alba, a metà nei passaggi")]
        public void Lights_OnFromDuskToDawn()
        {
            Assert.AreEqual(0f, DayCycle.Lights(DayCycle.Start(DayMoment.Day) + 10f));
            Assert.AreEqual(1f, DayCycle.Lights(DayCycle.Start(DayMoment.Dusk) + 10f));
            Assert.AreEqual(1f, DayCycle.Lights(DayCycle.Start(DayMoment.Night) + 10f));
            Assert.AreEqual(1f, DayCycle.Lights(DayCycle.Start(DayMoment.Dawn) + 10f));
            Assert.AreEqual(0.5f, DayCycle.Lights(DayCycle.Start(DayMoment.Dusk) - DayCycle.TransitionSeconds * 0.5f), 0.01f);
        }

        [Test, Description("La notte è la più buia, ma la luce ambiente non scende sotto il minimo: il borgo resta leggibile")]
        public void Night_IsDarkButReadable()
        {
            var lighting = AssetDatabase.LoadAssetAtPath<DayLighting>(LightingPath);
            var night = lighting.Look(DayMoment.Night);
            foreach (var moment in new[] { DayMoment.Dawn, DayMoment.Day, DayMoment.Dusk })
            {
                Assert.Less(night.SunIntensity, lighting.Look(moment).SunIntensity, $"{moment}");
                Assert.Less(lighting.Ambient(night).grayscale, lighting.Ambient(lighting.Look(moment)).grayscale, $"{moment}");
            }

            Assert.That(lighting.Ambient(night).grayscale, Is.GreaterThanOrEqualTo(lighting.MinimumAmbient.grayscale - 0.001f));
            Assert.That(lighting.MinimumAmbient.grayscale, Is.GreaterThan(0.08f), "un minimo che si vede");
        }

        // un numero che segue tutta la giornata: quanto del momento di arrivo, contato lungo il giro
        private static float Weight(float time)
        {
            var phase = DayCycle.At(time);
            return (int)phase.From + phase.Blend;
        }
    }
}

using DarkDescent.Rendering;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>Lo zoom della camera (D11 della M8): scatti del 10%, tra il 60% e il 140%, smorzato in 0,15 s.</summary>
    public class ZoomLevelTests
    {
        [Test, Description("Uno scatto in avanti avvicina del 10%, indietro allontana; oltre i limiti si ferma al 60% e al 140%")]
        public void Scroll_MovesByStepsWithinLimits()
        {
            var zoom = new ZoomLevel();
            Assert.AreEqual(1f, zoom.Target);

            Assert.IsTrue(zoom.Scroll(1));
            Assert.AreEqual(0.9f, zoom.Target, 1e-5f);
            Assert.IsTrue(zoom.Scroll(-2));
            Assert.AreEqual(1.1f, zoom.Target, 1e-5f);

            for (int i = 0; i < 20; i++)
            {
                zoom.Scroll(1);
            }

            Assert.AreEqual(0.6f, zoom.Target, 1e-5f);
            Assert.IsFalse(zoom.Scroll(1), "al limite uno scatto in più non cambia niente");

            for (int i = 0; i < 20; i++)
            {
                zoom.Scroll(-1);
            }

            Assert.AreEqual(1.4f, zoom.Target, 1e-5f);
            Assert.AreEqual(ZoomLevel.Max, zoom.Target, 1e-5f);
            Assert.AreEqual(0.6f, ZoomLevel.Min, 1e-5f);
        }

        [Test, Description("Il fattore arriva a quello scelto in 0,15 s, senza mai andare oltre; prima della fine è già quasi arrivato")]
        public void Advance_ReachesTargetInDuration()
        {
            var zoom = new ZoomLevel();
            zoom.Scroll(-3);
            Assert.AreEqual(1f, zoom.Current, "lo scatto non salta: parte da dove era");

            float previous = zoom.Current;
            for (int frame = 1; frame <= 8; frame++)
            {
                Assert.IsTrue(zoom.Advance(1f / 60f));
                Assert.Greater(zoom.Current, previous, "sale a ogni frame");
                Assert.Less(zoom.Current, 1.3f, "prima di 0,15 s non è ancora arrivato");
                previous = zoom.Current;
            }

            Assert.IsTrue(zoom.Advance(0.02f));
            Assert.AreEqual(1.3f, zoom.Current, 1e-5f, "a 0,15 s è arrivato");
            Assert.IsTrue(zoom.IsSettled);
            Assert.IsFalse(zoom.Advance(1f / 60f), "fermo, non cambia più");

            // rallenta verso la fine: a metà tempo ha fatto più di metà strada
            zoom.Scroll(3);
            zoom.Advance(ZoomLevel.Duration / 2f);
            Assert.Less(zoom.Current, 1.15f);
        }

        [Test, Description("Uno scatto durante lo smorzamento riparte da dove si è, senza salti")]
        public void Scroll_DuringAnimation_StartsFromCurrent()
        {
            var zoom = new ZoomLevel();
            zoom.Scroll(-1);
            zoom.Advance(0.05f);
            float midway = zoom.Current;
            Assert.Greater(midway, 1f);
            Assert.Less(midway, 1.1f);

            zoom.Scroll(-1);
            Assert.AreEqual(midway, zoom.Current, "nessun salto allo scatto");
            zoom.Advance(ZoomLevel.Duration);
            Assert.AreEqual(1.2f, zoom.Current, 1e-5f);
        }

        [Test, Description("Il fattore delle preferenze si applica subito, portato allo scatto più vicino e dentro i limiti")]
        public void Set_FromPreference_SnapsAndClamps()
        {
            var zoom = new ZoomLevel();
            zoom.Set(1.2f);
            Assert.AreEqual(1.2f, zoom.Current, 1e-5f);
            Assert.IsTrue(zoom.IsSettled);
            Assert.IsFalse(zoom.Advance(0.1f));

            zoom.Set(0.84f);
            Assert.AreEqual(0.8f, zoom.Target, 1e-5f);
            zoom.Set(5f);
            Assert.AreEqual(1.4f, zoom.Target, 1e-5f);
            zoom.Set(-3f);
            Assert.AreEqual(0.6f, zoom.Target, 1e-5f);
        }
    }
}

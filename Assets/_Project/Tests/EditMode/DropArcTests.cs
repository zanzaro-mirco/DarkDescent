using DarkDescent.Items;
using NUnit.Framework;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class DropArcTests
    {
        private static readonly Vector3 From = new Vector3(0f, 1f, 0f);
        private static readonly Vector3 To = new Vector3(1f, 0f, 0.5f);

        private static void AssertNear(Vector3 expected, Vector3 actual, string message)
        {
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"{message}: atteso {expected}, trovato {actual}");
        }

        [Test, Description("L'arco parte dal punto di lancio, tocca terra sul punto d'arrivo e alla fine del rimbalzo ci resta")]
        public void Arc_StartsAtFromAndEndsAtTo()
        {
            AssertNear(From, DropArc.Position(From, To, 1f, 0.1f, 0f), "partenza");
            AssertNear(To, DropArc.Position(From, To, 1f, 0.1f, DropArc.LandingTime), "atterraggio");
            AssertNear(To, DropArc.Position(From, To, 1f, 0.1f, 1f), "fine");
            AssertNear(To, DropArc.Position(From, To, 1f, 0.1f, 2f), "oltre la fine");
        }

        [Test, Description("A metà volo l'oggetto sta sopra la retta tra partenza e arrivo dell'altezza dell'arco")]
        public void Arc_RisesByItsHeightHalfway()
        {
            Vector3 middle = DropArc.Position(From, To, 1.2f, 0.1f, DropArc.LandingTime / 2f);
            AssertNear((From + To) / 2f + Vector3.up * 1.2f, middle, "metà volo");
        }

        [Test, Description("Dopo l'atterraggio un rimbalzo piccolo, sul posto, alto quanto chiesto")]
        public void Bounce_StaysInPlaceAndIsSmall()
        {
            Vector3 top = DropArc.Position(From, To, 1f, 0.15f, (1f + DropArc.LandingTime) / 2f);
            AssertNear(To + Vector3.up * 0.15f, top, "cima del rimbalzo");
        }

        [Test, Description("La capriola parte da giri interi indietro e finisce a zero quando tocca terra, e lì resta")]
        public void Spin_EndsFlatOnLanding()
        {
            Assert.AreEqual(-360f, DropArc.Spin(1f, 0f), 1e-3f);
            Assert.AreEqual(-180f, DropArc.Spin(1f, DropArc.LandingTime / 2f), 1e-3f);
            Assert.AreEqual(0f, DropArc.Spin(1f, DropArc.LandingTime), 1e-3f);
            Assert.AreEqual(0f, DropArc.Spin(1f, 1f), 1e-3f);
        }
    }
}

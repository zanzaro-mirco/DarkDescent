using DarkDescent.Town;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>Il saluto degli abitanti del borgo (D9 della M18).</summary>
    public class GreetingTests
    {
        [Test, Description("Fermo finché non arriva nessuno; poi il saluto sale in un blend, tiene, scende e finisce dopo la sua durata")]
        public void Greeting_RisesHoldsAndFades()
        {
            var greeting = new Greeting(4f, 0.5f);
            greeting.Tick(10f);
            Assert.AreEqual(0f, greeting.Weight);
            Assert.IsFalse(greeting.IsWaving);

            greeting.Begin();
            Assert.AreEqual(0f, greeting.Weight);
            greeting.Tick(0.25f);
            Assert.AreEqual(0.5f, greeting.Weight, 1e-4f, "a metà della salita");
            greeting.Tick(1f);
            Assert.AreEqual(1f, greeting.Weight);
            greeting.Tick(2.5f);
            Assert.AreEqual(0.5f, greeting.Weight, 1e-4f, "a metà della discesa");
            greeting.Tick(0.3f);
            Assert.IsFalse(greeting.IsWaving);
            Assert.AreEqual(0f, greeting.Weight);
        }

        [Test, Description("Un arrivo a metà della discesa riparte dal peso che c'è, senza salti, e il saluto dura di nuovo per intero")]
        public void Greeting_RestartsWithoutJumps()
        {
            var greeting = new Greeting(4f, 0.5f);
            greeting.Begin();
            greeting.Tick(3.75f);
            float before = greeting.Weight;
            greeting.Begin();
            Assert.AreEqual(before, greeting.Weight, 1e-4f);
            greeting.Tick(3f);
            Assert.AreEqual(1f, greeting.Weight, "di nuovo per intero");
        }

        [Test, Description("Una durata più corta di due blend si allunga: il saluto arriva sempre a peso pieno")]
        public void Greeting_AlwaysReachesFullWeight()
        {
            var greeting = new Greeting(0.1f, 0.5f);
            greeting.Begin();
            greeting.Tick(0.5f);
            Assert.AreEqual(1f, greeting.Weight, 1e-4f);
        }
    }
}

using DarkDescent.Audio;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class SfxBudgetTests
    {
        [Test, Description("Un suono per tipo per fotogramma: il secondo impatto dello stesso fotogramma tace, un tipo diverso no, il fotogramma dopo sì")]
        public void OnePerKindPerFrame()
        {
            var budget = new SfxBudget(12);
            Assert.IsTrue(budget.Request(SfxKind.Hit, 10, 0f, 3f, 0.5f));
            for (int i = 0; i < 9; i++)
            {
                Assert.IsFalse(budget.Request(SfxKind.Hit, 10, 0f, 2f, 0.5f), "dieci impatti insieme suonano come uno");
            }

            Assert.IsTrue(budget.Request(SfxKind.Death, 10, 0f, 3f, 0.5f), "un altro tipo passa");
            Assert.IsTrue(budget.Request(SfxKind.CriticalVoice, 10, 0f, 3f, 0.5f), "anche il verso del critico");
            Assert.IsTrue(budget.Request(SfxKind.Hit, 11, 0.02f, 3f, 0.5f), "il fotogramma dopo");
            Assert.AreEqual(4, budget.ActiveVoices);
        }

        [Test, Description("Oltre il limite cade il più lontano: un suono più lontano di tutti tace, uno più vicino prende il posto del più lontano")]
        public void OverTheLimit_TheFarthestGoes()
        {
            var budget = new SfxBudget(3);
            Assert.IsTrue(budget.Request(SfxKind.Hit, 1, 0f, 5f, 10f));
            Assert.IsTrue(budget.Request(SfxKind.Hit, 2, 0f, 10f, 10f));
            Assert.IsTrue(budget.Request(SfxKind.Hit, 3, 0f, 15f, 10f));
            Assert.AreEqual(3, budget.ActiveVoices);

            Assert.IsFalse(budget.Request(SfxKind.Hit, 4, 0f, 20f, 10f), "più lontano di tutti: tace");
            Assert.IsFalse(budget.Request(SfxKind.Hit, 5, 0f, 15f, 10f), "alla pari con il più lontano: tace");
            Assert.IsTrue(budget.Request(SfxKind.Hit, 6, 0f, 1f, 10f), "vicino: prende il posto di quello a 15 m");
            Assert.AreEqual(3, budget.ActiveVoices);
            Assert.IsFalse(budget.Request(SfxKind.Hit, 7, 0f, 12f, 10f), "ora il più lontano è a 10 m");
        }

        [Test, Description("I suoni finiti liberano il posto")]
        public void FinishedSounds_FreeTheirVoice()
        {
            var budget = new SfxBudget(2);
            Assert.IsTrue(budget.Request(SfxKind.Hit, 1, 0f, 5f, 0.5f));
            Assert.IsTrue(budget.Request(SfxKind.Swing, 1, 0f, 6f, 2f));
            Assert.IsFalse(budget.Request(SfxKind.Death, 2, 0.4f, 30f, 1f), "pieno, e lontano");
            Assert.IsTrue(budget.Request(SfxKind.Death, 3, 0.6f, 30f, 1f), "l'impatto è finito a 0,5 s");
            Assert.AreEqual(2, budget.ActiveVoices);
        }

        [Test, Description("La priorità di Unity scende con la distanza (0 è la più alta) e resta tra 0 e 255")]
        public void Priority_FromDistance()
        {
            Assert.AreEqual(64, SfxBudget.Priority(0f));
            Assert.Less(SfxBudget.Priority(2f), SfxBudget.Priority(10f));
            Assert.AreEqual(255, SfxBudget.Priority(1000f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SfxBudget(0));
        }
    }
}

using DarkDescent.Films;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>Il salto di un filmato con due Esc o due click (D9 della M19).</summary>
    public class SkipPromptTests
    {
        [Test, Description("Il primo tasto mostra l'invito, il secondo mentre si vede salta")]
        public void SecondPress_WhileShowing_Skips()
        {
            var prompt = new SkipPrompt(3f);
            Assert.IsFalse(prompt.IsShowing(0f));

            prompt.Press(1f);
            Assert.IsTrue(prompt.IsShowing(1.5f));
            Assert.IsFalse(prompt.Skipped);

            prompt.Press(1.8f);
            Assert.IsTrue(prompt.Skipped);
            Assert.IsFalse(prompt.IsShowing(1.9f), "saltato, l'invito sparisce");
        }

        [Test, Description("L'invito sparisce da solo, e il tasto dopo lo rimostra invece di saltare")]
        public void Prompt_FadesAndShowsAgain()
        {
            var prompt = new SkipPrompt(3f);
            prompt.Press(0f);
            Assert.IsFalse(prompt.IsShowing(3f));

            prompt.Press(5f);
            Assert.IsFalse(prompt.Skipped, "l'invito non c'era più");
            Assert.IsTrue(prompt.IsShowing(5.1f));
        }
    }
}

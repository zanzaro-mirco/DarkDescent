using NUnit.Framework;

namespace DarkDescent.Tests
{
    // Test rotto di proposito (passo 2.5.3): serve solo a vedere il check rosso in una PR.
    // Il branch non va mai unito a main.
    public class RedCheckTests
    {
        [Test, Description("Fallisce sempre: prova che la CI intercetta un test rotto")]
        public void AlwaysFails()
        {
            Assert.Fail("Test rotto di proposito per la prova del rosso in CI");
        }
    }
}

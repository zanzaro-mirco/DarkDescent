using System;
using System.IO;
using System.Linq;
using DarkDescent.Core;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>Il rapporto della ronda notturna (D8 della M15): dove si scrive e com'è fatta una parte.</summary>
    public class NightlyReportTests
    {
        [Test, Description("Il percorso viene dalla riga di comando, poi dalla variabile; senza, nessun rapporto")]
        public void FilePath_ComesFromArgumentsOrVariable()
        {
            var args = new[] { "Unity.exe", NightlyReport.PathOption, "artifacts/rapporto.md" };
            Assert.AreEqual("artifacts/rapporto.md", NightlyReport.FilePath(args, "altro.md"));
            Assert.AreEqual("altro.md", NightlyReport.FilePath(new[] { "Unity.exe" }, " altro.md "));
            Assert.IsNull(NightlyReport.FilePath(new[] { "Unity.exe" }, null));
            Assert.IsNull(NightlyReport.FilePath(new[] { "Unity.exe" }, "  "));
        }

        [Test, Description("Una parte verde: il titolo, la spunta con i numeri e i dettagli, nessun blocco di errori")]
        public void GreenPart_HasNoErrors()
        {
            string part = NightlyReport.Part("Mappe della cripta", "2000 livelli, 0 con errori, in 9 s", Array.Empty<string>(), "| a | b |\n|---|---|\n");
            Assert.AreEqual("### Mappe della cripta\n\n✅ 2000 livelli, 0 con errori, in 9 s\n\n| a | b |\n|---|---|\n\n", part);
        }

        [Test, Description("Una parte con errori: la croce, e i primi dieci in un blocco che si apre, con il testo com'è; gli altri solo contati")]
        public void RedPart_ShowsTheFirstErrors()
        {
            var errors = Enumerable.Range(1, 12).Select(i => $"partita {i}, profondità 8: errore\n##.#\n#..#").ToList();
            string part = NightlyReport.Part("Mappe delle caverne", "2000 livelli, 12 con errori", errors);
            StringAssert.StartsWith("### Mappe delle caverne\n\n❌ 2000 livelli, 12 con errori\n\n<details><summary>12 errori, i primi 10</summary>\n\n```\n", part);
            StringAssert.Contains("partita 10, profondità 8: errore\n##.#\n#..#\n```\n\n</details>", part);
            StringAssert.DoesNotContain("partita 11", part);
            StringAssert.Contains("<summary>1 errore</summary>", NightlyReport.Part("x", "y", new[] { "uno" }));
        }

        [Test, Description("Le parti si aggiungono una dopo l'altra, e la cartella si crea se manca")]
        public void Append_AddsToTheFile()
        {
            string folder = Path.Combine(Path.GetTempPath(), "DarkDescentReport_" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "artifacts", "rapporto.md");
            try
            {
                NightlyReport.Append(path, "### Uno\n\n");
                NightlyReport.Append(path, "### Due\n\n");
                Assert.AreEqual("### Uno\n\n### Due\n\n", File.ReadAllText(path));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}

using System.IO;
using System.Linq;
using DarkDescent.Progression;
using DarkDescent.UI;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    /// <summary>La schermata di vittoria e i riconoscimenti (D9 della M10).</summary>
    public class VictoryTests
    {
        private static string Credits => File.ReadAllText("CREDITS.md");

        [Test, Description("Il tempo di gioco: minuti e secondi sotto l'ora, poi anche le ore")]
        public void FormatTime_ReadsLikeAClock()
        {
            Assert.AreEqual("0:00", RunStats.FormatTime(0f));
            Assert.AreEqual("0:00", RunStats.FormatTime(-3f));
            Assert.AreEqual("1:05", RunStats.FormatTime(65.9f));
            Assert.AreEqual("59:59", RunStats.FormatTime(3599f));
            Assert.AreEqual("1:02:05", RunStats.FormatTime(3725f));
        }

        [Test, Description("I riconoscimenti leggono la tabella di CREDITS.md: opera e autore di ogni asset di terzi, senza markdown, e non le righe fatte per il progetto")]
        public void CreditsTable_ReadsTheThirdPartyRows()
        {
            var entries = CreditsTable.Parse(Credits);
            int rows = Credits.Split('\n').Count(l => l.StartsWith("| ") && !l.StartsWith("| Asset"));
            int own = Credits.Split('\n').Count(l => l.StartsWith("| ") && l.Split('|')[2].Trim().StartsWith("creat"));
            Assert.AreEqual(rows - own, entries.Count);
            Assert.Greater(own, 0);

            Assert.IsTrue(entries.Any(e => e.Work == "KayKit — Character Pack: Skeletons 1.1" && e.Author == "Kay Lousberg"));
            Assert.IsTrue(entries.Any(e => e.Work == "Dungeon Ambience" && e.Author == "yd" && e.License == "CC0 1.0"));
            Assert.IsTrue(entries.Any(e => e.Work == "Medieval: The Bard's Tale" && e.Author == "RandomMind"));
            Assert.IsTrue(entries.Any(e => e.Work == "Cinzel" && e.Author == "The Cinzel Project Authors (Natanael Gama)"));
            Assert.IsTrue(entries.Any(e => e.Work == "LiberationSans" && e.Author == "Google Corporation, Red Hat, Inc."));
            foreach (var entry in entries)
            {
                foreach (string field in new[] { entry.Work, entry.Author, entry.License })
                {
                    Assert.IsFalse(string.IsNullOrEmpty(field), entry.Work);
                    StringAssert.DoesNotContain("[", field);
                    StringAssert.DoesNotContain("*", field);
                    StringAssert.DoesNotContain("`", field);
                    StringAssert.DoesNotContain("http", field);
                }
            }
        }

        [Test, Description("La copia di CREDITS.md tra gli asset è uguale all'originale: DarkDescent → Aggiorna i riconoscimenti")]
        public void CreditsCopy_IsUpToDate()
        {
            string copy = File.ReadAllText("Assets/_Project/Data/Credits.txt").Replace("\r\n", "\n");
            Assert.AreEqual(Credits.Replace("\r\n", "\n"), copy, "dopo aver cambiato CREDITS.md: DarkDescent → Aggiorna i riconoscimenti");
        }
    }
}

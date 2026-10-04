using System;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Localization;
using NUnit.Framework;

namespace DarkDescent.Tests
{
    public class LocalizationTests
    {
        private const string Csv =
            "key,en,it\n" +
            "# un commento\n" +
            "\n" +
            "plain,Hello,Ciao\n" +
            "comma,\"Damage, physical\",\"Danno, fisico\"\n" +
            "quotes,\"Say \"\"hi\"\"\",\"Di' \"\"ciao\"\"\"\n" +
            "lines,One\\nTwo,Uno\\nDue\n" +
            "only_en,Only English,\n" +
            "format,Level {0},Livello {0}\n";

        private static Localizer Create(string language = "en") => new Localizer(StringTable.Parse(Csv), language);

        [Test, Description("Virgole tra virgolette, virgolette raddoppiate, \\n che va a capo, commenti e righe vuote ignorati")]
        public void Parse_HandlesQuotesCommasAndNewlines()
        {
            var table = StringTable.Parse(Csv);

            CollectionAssert.AreEqual(new[] { "en", "it" }, table.Languages);
            Assert.AreEqual(6, table.Count);
            Assert.IsTrue(table.TryGet("comma", 1, out var comma));
            Assert.AreEqual("Danno, fisico", comma);
            Assert.IsTrue(table.TryGet("quotes", 0, out var quotes));
            Assert.AreEqual("Say \"hi\"", quotes);
            Assert.IsTrue(table.TryGet("lines", 1, out var lines));
            Assert.AreEqual("Uno\nDue", lines);
        }

        [Test, Description("Chiavi ripetute, virgolette non chiuse e intestazione sbagliata sono errori, non testi strani")]
        public void Parse_RejectsBrokenTables()
        {
            Assert.Throws<FormatException>(() => StringTable.Parse("key,en\na,1\na,2\n"));
            Assert.Throws<FormatException>(() => StringTable.Parse("key,en\na,\"open\n"));
            Assert.Throws<FormatException>(() => StringTable.Parse("id,en\na,1\n"));
        }

        [Test, Description("La lingua attiva risponde; una traduzione mancante ricade sull'inglese; una chiave che non c'è si vede")]
        public void Get_FallsBackToEnglish_MarksMissingKeys()
        {
            var localizer = Create("it");

            Assert.AreEqual("Ciao", localizer.Get("plain"));
            Assert.AreEqual("Only English", localizer.Get("only_en"));
            Assert.AreEqual("#nope", localizer.Get("nope"));
            Assert.AreEqual("Livello 3", localizer.Format("format", 3));
        }

        [Test, Description("Cambiare lingua avvisa chi ascolta; una lingua sconosciuta non cambia niente")]
        public void SetLanguage_RaisesEventOnlyOnChange()
        {
            var localizer = Create();
            int changes = 0;
            localizer.LanguageChanged += () => changes++;

            Assert.IsFalse(localizer.SetLanguage("fr"));
            Assert.IsTrue(localizer.SetLanguage("en"));
            Assert.AreEqual(0, changes, "già in inglese");

            localizer.CycleLanguage();
            Assert.AreEqual("it", localizer.Language);
            localizer.CycleLanguage();
            Assert.AreEqual("en", localizer.Language, "dall'ultima lingua alla prima");
            Assert.AreEqual(2, changes);
        }

        [Test, Description("Le opzioni da riga di comando: -lang it, maiuscole indifferenti, opzione senza valore")]
        public void CommandLine_ReadsOptionValues()
        {
            var args = new[] { "DarkDescent.exe", "-LANG", "it", "-seed" };

            Assert.IsTrue(CommandLine.TryGetValue(args, "-lang", out var language));
            Assert.AreEqual("it", language);
            Assert.IsFalse(CommandLine.TryGetValue(args, "-seed", out _), "manca il valore");
            Assert.IsFalse(CommandLine.TryGetValue(args.Take(1).ToArray(), "-lang", out _));
        }
    }
}

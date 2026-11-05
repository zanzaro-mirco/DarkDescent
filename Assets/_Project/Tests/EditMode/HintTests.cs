using System.Linq;
using System.Text.RegularExpressions;
using DarkDescent.Localization;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    /// <summary>I suggerimenti del primo gioco (D11–D13 della M23): la memoria, l'ordine, i testi nelle due lingue.</summary>
    public class HintTests
    {
        private static StringTable Table => StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);

        [Test, Description("Sono dieci, tutti diversi, e il primo è il muoversi")]
        public void Hints_AreTen()
        {
            Assert.AreEqual(10, Hints.All.Count);
            Assert.AreEqual(10, Hints.All.Distinct().Count());
            Assert.AreEqual(Hints.Move, Hints.All[0]);
        }

        [Test, Description("Un suggerimento entra in coda una volta sola e, mostrato, non torna più")]
        public void Book_ShowsEachHintOnlyOnce()
        {
            var book = new HintBook();
            Assert.IsTrue(book.Raise(Hints.Enemy));
            Assert.IsFalse(book.Raise(Hints.Enemy), "già in coda");

            Assert.IsTrue(book.TryNext(out string shown));
            Assert.AreEqual(Hints.Enemy, shown);
            Assert.IsTrue(book.HasSeen(Hints.Enemy));
            Assert.IsFalse(book.Raise(Hints.Enemy), "già visto");
            Assert.IsFalse(book.TryNext(out _), "la coda è vuota");
        }

        [Test, Description("Due suggerimenti insieme escono nell'ordine della lista, qualunque sia l'ordine di arrivo")]
        public void Book_ReleasesInThePriorityOrder()
        {
            var book = new HintBook();
            book.Raise(Hints.Village);
            book.Raise(Hints.Item);
            book.Raise(Hints.Move);
            book.Raise(Hints.Portal);

            var order = new System.Collections.Generic.List<string>();
            while (book.TryNext(out string id))
            {
                order.Add(id);
            }

            CollectionAssert.AreEqual(new[] { Hints.Move, Hints.Item, Hints.Portal, Hints.Village }, order);
        }

        [Test, Description("Spenti, niente entra in coda e quello che aspettava sparisce; riaccesi, i non visti tornano a poter scattare")]
        public void Book_TurnedOff_QueuesNothing()
        {
            var book = new HintBook();
            book.Raise(Hints.Enemy);
            book.Enabled = false;
            Assert.AreEqual(0, book.QueuedCount, "la coda si svuota");
            Assert.IsFalse(book.Raise(Hints.Item));
            Assert.IsFalse(book.TryNext(out _));

            book.Enabled = true;
            Assert.IsFalse(book.HasSeen(Hints.Enemy), "non era stato mostrato: può ancora scattare");
            Assert.IsTrue(book.Raise(Hints.Enemy));
        }

        [Test, Description("«Rivedi i suggerimenti» li azzera tutti e svuota la coda")]
        public void Book_Reset_MakesEverythingNewAgain()
        {
            var book = new HintBook();
            book.Raise(Hints.Move);
            book.TryNext(out _);
            book.Raise(Hints.Enemy);

            book.Reset();

            Assert.AreEqual(0, book.SeenCount);
            Assert.AreEqual(0, book.QueuedCount);
            Assert.IsTrue(book.Raise(Hints.Move));
        }

        [Test, Description("La memoria si salva come una riga e si rilegge uguale; righe vuote o sporche non fanno danno")]
        public void Book_SurvivesSaving()
        {
            var book = new HintBook();
            book.Raise(Hints.Potion);
            book.Raise(Hints.Move);
            book.TryNext(out _);
            book.TryNext(out _);
            Assert.AreEqual("move,potion", book.Serialize(), "in ordine di precedenza, stabile");

            var again = new HintBook();
            again.Load(book.Serialize());
            Assert.IsTrue(again.HasSeen(Hints.Move));
            Assert.IsTrue(again.HasSeen(Hints.Potion));
            Assert.IsFalse(again.HasSeen(Hints.Enemy));
            Assert.IsFalse(again.Raise(Hints.Move), "già visto");

            again.Load(null);
            Assert.AreEqual(0, again.SeenCount);
            again.Load(" , move ,, ");
            Assert.AreEqual(1, again.SeenCount);
        }

        [Test, Description("Ogni suggerimento ha titolo e testo, in inglese e in italiano, e nessuno dei due è vuoto")]
        public void EveryHint_HasItsTextsInBothLanguages()
        {
            var table = Table;
            foreach (string id in Hints.All)
            {
                foreach (string key in new[] { Hints.TitleKey(id), Hints.TextKey(id) })
                {
                    Assert.IsTrue(table.Contains(key), "manca " + key);
                    for (int language = 0; language < table.Languages.Count; language++)
                    {
                        Assert.IsTrue(table.TryGet(key, language, out string value) && !string.IsNullOrWhiteSpace(value), $"{key} in {table.Languages[language]} è vuoto");
                    }
                }
            }
        }

        [Test, Description("I segnaposto dei tasti, {0} {1} {2}, sono gli stessi nelle due lingue, e nessuno più di tre: il testo non resta con un buco")]
        public void HintTexts_HaveTheSamePlaceholdersInBothLanguages()
        {
            var table = Table;
            foreach (string id in Hints.All)
            {
                string key = Hints.TextKey(id);
                var found = new System.Collections.Generic.List<string>();
                for (int language = 0; language < table.Languages.Count; language++)
                {
                    table.TryGet(key, language, out string value);
                    var marks = Regex.Matches(value, @"\{(\d+)\}").Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x).ToArray();
                    Assert.IsTrue(marks.All(m => int.Parse(m) <= 2), $"{key}: solo {{0}}, {{1}} e {{2}}");
                    found.Add(string.Join(",", marks));
                }

                Assert.AreEqual(1, found.Distinct().Count(), $"{key}: segnaposto diversi tra le lingue ({string.Join(" | ", found)})");
            }
        }

        [Test, Description("Un testo si scrive con tre tasti anche se ne usa uno: nessun errore di formato")]
        public void HintTexts_FormatWithThreeKeys()
        {
            var localizer = new Localizer(Table);
            foreach (string language in new[] { "en", "it" })
            {
                Assert.IsTrue(localizer.SetLanguage(language));
                foreach (string id in Hints.All)
                {
                    string text = localizer.Format(Hints.TextKey(id), "A", "B", "C");
                    Assert.IsFalse(text.Contains("{"), $"{id} in {language}: segnaposto rimasto");
                }
            }
        }

        [Test, Description("Le opzioni dei suggerimenti hanno il loro testo nelle due lingue")]
        public void Options_HaveTheirTexts()
        {
            var table = Table;
            foreach (string key in new[] { "options.hints", "options.hints_review" })
            {
                Assert.IsTrue(table.Contains(key), key);
            }
        }
    }
}

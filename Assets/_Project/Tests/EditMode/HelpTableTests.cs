using System;
using System.Linq;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DarkDescent.Localization;

namespace DarkDescent.Tests
{
    /// <summary>Il pannello dei comandi (D14 della M23) come dati: i gruppi, i tasti, i testi nelle due lingue.</summary>
    public class HelpTableTests
    {
        private static StringTable Table => StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);

        [Test, Description("Quattro gruppi in due colonne: Movimento e Vedere a sinistra, Combattimento e Finestre a destra")]
        public void Table_HasFourGroupsInTwoColumns()
        {
            CollectionAssert.AreEqual(new[] { "help.group.move", "help.group.see" }, HelpTable.Left.Select(g => g.TitleKey));
            CollectionAssert.AreEqual(new[] { "help.group.combat", "help.group.windows" }, HelpTable.Right.Select(g => g.TitleKey));
        }

        [Test, Description("Ogni tasto dell'elenco compare almeno in una riga, e le righe sono diciassette")]
        public void Table_UsesEveryKeyKind()
        {
            var rows = HelpTable.Left.Concat(HelpTable.Right).SelectMany(g => g.Rows).ToList();
            Assert.AreEqual(17, rows.Count);
            foreach (HelpKey key in Enum.GetValues(typeof(HelpKey)))
            {
                Assert.IsTrue(rows.Any(r => r.Key == key), key + " non è in nessuna riga");
            }
        }

        [Test, Description("Ogni testo del pannello c'è nelle due lingue e non è vuoto")]
        public void EveryText_ExistsInBothLanguages()
        {
            var table = Table;
            foreach (string key in HelpTable.TextKeys().Concat(new[] { "pause.help" }))
            {
                Assert.IsTrue(table.Contains(key), "manca " + key);
                for (int language = 0; language < table.Languages.Count; language++)
                {
                    Assert.IsTrue(table.TryGet(key, language, out string value) && !string.IsNullOrWhiteSpace(value), $"{key} in {table.Languages[language]} è vuoto");
                }
            }
        }

        [Test, Description("Solo la riga del tasto sinistro sul nemico cambia con il Mago (il dardo arcano); le altre sono uguali per tutti")]
        public void OnlyTheAttackRow_ChangesForTheMage()
        {
            var rows = HelpTable.Left.Concat(HelpTable.Right).SelectMany(g => g.Rows).ToList();
            var changing = rows.Where(r => r.MageTextKey != null).ToList();
            Assert.AreEqual(1, changing.Count);
            Assert.AreEqual(HelpKey.LeftClick, changing[0].Key);
            Assert.AreNotEqual(changing[0].TextFor(false), changing[0].TextFor(true));
            foreach (var row in rows.Where(r => r.MageTextKey == null))
            {
                Assert.AreEqual(row.TextFor(false), row.TextFor(true));
            }
        }

        [Test, Description("Il testo del Mago nomina il dardo, in tutte e due le lingue")]
        public void MageText_NamesTheBolt()
        {
            var table = Table;
            table.TryGet("help.row.attack.mage", table.IndexOf("en"), out string english);
            table.TryGet("help.row.attack.mage", table.IndexOf("it"), out string italian);
            StringAssert.Contains("arcane bolt", english);
            StringAssert.Contains("dardo arcano", italian);
        }
    }
}

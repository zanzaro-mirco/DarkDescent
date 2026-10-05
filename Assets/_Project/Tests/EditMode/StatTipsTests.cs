using DarkDescent.Localization;
using DarkDescent.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class StatTipsTests
    {
        [Test, Description("Ogni riga del pannello del personaggio ha la sua spiegazione nelle due lingue, e la riga vuota nessuna (D16)")]
        public void EveryStatLine_HasItsTip()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Localization/Strings.csv");
            var table = StringTable.Parse(csv.text);
            foreach (var language in new[] { "en", "it" })
            {
                Assert.IsTrue(table.TryGet("hud.stat_names", table.IndexOf(language), out string names));
                string[] lines = names.Split('\n');
                Assert.AreEqual(CharacterPanel.LineTips.Length, lines.Length, language);
                for (int i = 0; i < lines.Length; i++)
                {
                    string tip = CharacterPanel.LineTips[i];
                    Assert.AreEqual(string.IsNullOrEmpty(lines[i]), tip == null, $"{language}, riga {i}: «{lines[i]}»");
                    if (tip != null)
                    {
                        Assert.IsTrue(table.TryGet(tip, table.IndexOf(language), out string text) && text.Length > 0, $"{tip} in {language}");
                    }
                }
            }
        }
    }
}

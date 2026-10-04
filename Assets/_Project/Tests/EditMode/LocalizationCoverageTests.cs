using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using DarkDescent.Items;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkDescent.Tests
{
    public class LocalizationCoverageTests
    {
        private const string TablePath = "Assets/_Project/Data/Localization/Strings.csv";

        private static StringTable Table => StringTable.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(TablePath).text);

        // le chiavi scritte nelle scene e nei prefab: quelle delle etichette fisse e delle scale
        private static IEnumerable<(string file, string key)> SerializedKeys()
        {
            var pattern = new Regex(@"^\s*_(?:key|labelKey): (\S+)\s*$", RegexOptions.Multiline);
            foreach (var guid in AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Match match in pattern.Matches(File.ReadAllText(path)))
                {
                    yield return (path, match.Groups[1].Value);
                }
            }
        }

        [Test, Description("Ogni chiave del codice è nella tabella")]
        public void CodeKeys_Exist()
        {
            var table = Table;
            foreach (var field in typeof(TextKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                Assert.IsTrue(table.Contains((string)field.GetValue(null)), $"TextKeys.{field.Name} manca nella tabella");
            }
        }

        [Test, Description("Ogni oggetto, armi dei nemici comprese, ha il nome nella tabella")]
        public void ItemNameKeys_Exist()
        {
            var table = Table;
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project/Data" }))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(table.Contains(item.NameKey), $"{item.name}: chiave {item.NameKey} mancante");
            }
        }

        [Test, Description("Ogni chiave scritta nelle scene e nei prefab è nella tabella, e ce n'è almeno una")]
        public void SerializedKeys_Exist()
        {
            var table = Table;
            int count = 0;
            foreach (var (file, key) in SerializedKeys())
            {
                count++;
                Assert.IsTrue(table.Contains(key), $"{file}: chiave {key} mancante");
            }

            Assert.Greater(count, 5, "le etichette dell'HUD devono avere la loro chiave");
        }

        [Test, Description("Ogni lingua ha tutte le righe: niente ricadute sull'inglese nel gioco vero")]
        public void EveryLanguage_IsComplete()
        {
            var table = Table;
            Assert.GreaterOrEqual(table.Languages.Count, 2);
            foreach (var key in table.Keys)
            {
                for (int i = 0; i < table.Languages.Count; i++)
                {
                    Assert.IsTrue(table.TryGet(key, i, out _), $"{key}: manca la traduzione {table.Languages[i]}");
                }
            }
        }
    }
}

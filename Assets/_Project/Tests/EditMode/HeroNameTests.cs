using DarkDescent.Classes;
using DarkDescent.Editor;
using DarkDescent.Localization;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    /// <summary>Il nome del personaggio (D8 della M21) e i testi delle classi per la creazione.</summary>
    public class HeroNameTests
    {
        [TestCase("Mirco", "Mirco")]
        [TestCase("  Mirco  ", "Mirco")]
        [TestCase("Mirco   il   Grigio", "Mirco il Grigio")]
        [TestCase("<b>Mirco</b>", "bMirco/b")]
        [TestCase("Mir\tco\n", "Mir co")]
        [TestCase("Abcdefghijklmnopqrstuvwxyz", "Abcdefghijklmnop")]
        [TestCase("Abcdefghijklmno qrs", "Abcdefghijklmno")]
        [Description("Il nome si ripulisce: spazi ai bordi e di mezzo, niente < e > (il testo dei posti è ricco), al massimo 16 lettere senza spazio in coda")]
        public void Clean_FollowsTheRules(string raw, string expected)
        {
            Assert.AreEqual(expected, HeroName.Clean(raw, "Aldric"));
            Assert.LessOrEqual(HeroName.Clean(raw, "Aldric").Length, HeroName.MaxLength);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("<>")]
        [Description("Senza niente di valido torna il nome di partenza della classe")]
        public void Clean_FallsBackToTheDefault(string raw)
        {
            Assert.AreEqual("Aldric", HeroName.Clean(raw, "Aldric"));
        }

        [Test, Description("Ogni classe che si sceglie ha nome, descrizione e nome di partenza in tutte e due le lingue; il Mago ha la bacchetta e il tomo, il Guerriero niente")]
        public void PlayableClasses_HaveTheirTextsAndKit()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterClassCatalog>(ClassTools.CatalogPath);
            var table = StringTable.Parse(AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/_Project/Data/Localization/Strings.csv").text);
            int count = 0;
            foreach (var characterClass in catalog.Playable)
            {
                count++;
                foreach (string key in new[] { characterClass.NameKey, characterClass.DescriptionKey, characterClass.DefaultNameKey })
                {
                    Assert.IsTrue(table.Contains(key), $"{characterClass.Id}: manca {key}");
                    foreach (string language in new[] { "en", "it" })
                    {
                        Assert.IsTrue(table.TryGet(key, table.IndexOf(language), out string text) && text.Length > 0, $"{key} in {language}");
                    }
                }

                string defaultName = null;
                table.TryGet(characterClass.DefaultNameKey, table.IndexOf("en"), out defaultName);
                Assert.LessOrEqual(defaultName.Length, HeroName.MaxLength, characterClass.Id);
            }

            Assert.AreEqual(2, count);
            var mage = catalog.Find("mage");
            Assert.AreEqual(("Wand", "Tome"), (mage.StartingWeapon.name, mage.StartingOffhand.name));
            Assert.IsNull(catalog.Warrior.StartingWeapon);
        }
    }
}

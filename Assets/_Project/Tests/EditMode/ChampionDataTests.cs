using System.IO;
using System.Linq;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class ChampionDataTests
    {
        private static ChampionSettings Settings => AssetDatabase.LoadAssetAtPath<ChampionSettings>("Assets/_Project/Data/Enemies/ChampionSettings.asset");

        [Test, Description("I cinque modificatori della D5, con id diversi e un nome nelle due lingue")]
        public void Modifiers_HaveIdsAndNames()
        {
            var settings = Settings;
            Assert.AreEqual(5, settings.Modifiers.Count);
            var ids = settings.CollectModifierIds();
            CollectionAssert.AreEquivalent(new[] { "fast", "frozen", "regenerating", "unstoppable", "enraged" }, ids);
            var strings = File.ReadAllText("Assets/_Project/Data/Localization/Strings.csv");
            foreach (var modifier in settings.Modifiers)
            {
                Assert.AreSame(modifier, settings.Find(modifier.Id));
                StringAssert.Contains("\n" + modifier.NameKey + ",", strings, modifier.Id);
            }

            Assert.IsNull(settings.Find("nonsense"));
        }

        [Test, Description("Campioni e unici sono più forti dei normali, gli unici più dei campioni; i normali non cambiano")]
        public void Ranks_AreStrongerThanNormal()
        {
            var settings = Settings;
            var champion = settings.For(EnemyRank.Champion);
            var unique = settings.For(EnemyRank.Unique);
            Assert.IsNull(settings.For(EnemyRank.Normal));
            Assert.AreEqual(3f, champion.Life);
            Assert.AreEqual(1.5f, champion.Damage);
            Assert.AreEqual(4f, champion.Experience);
            Assert.Greater(unique.Life, champion.Life);
            Assert.Greater(unique.Damage, champion.Damage);
            Assert.Greater(unique.Experience, champion.Experience);
            Assert.Greater(champion.Scale, 1f);
        }

        [Test, Description("La cripta ha un gruppo di campioni a livello, le caverne due (D3); tutti dalla profondità 2")]
        public void LevelSettings_HaveTheirGroups()
        {
            var crypt = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");
            var caves = AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");
            Assert.AreEqual(1, crypt.Specials.Groups);
            Assert.AreEqual(2, caves.Specials.Groups);
            Assert.AreEqual(2, crypt.Specials.MinDepth);
            Assert.AreEqual(1, crypt.Specials.ModifiersAt(4));
            Assert.AreEqual(2, caves.Specials.ModifiersAt(5));
            Assert.AreEqual('S', caves.Specials.UniqueEscort);
        }

        [Test, Description("Il lampo del colpo non legge _BaseColor: la tinta di campioni, unici e cavaliere rallentato, data con un MaterialPropertyBlock, non lo colora")]
        public void HitFlash_IgnoresTheTint()
        {
            var flash = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/_Project/Art/Materials/M_HitFlash.mat");
            Assert.IsFalse(flash.HasProperty("_BaseColor"), "con _BaseColor il lampo di Morvak sarebbe rosso ruggine");
            Assert.AreEqual(UnityEngine.Color.white, flash.color);
        }
    }
}

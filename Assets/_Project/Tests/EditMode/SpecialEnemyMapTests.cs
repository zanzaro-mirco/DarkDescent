using System.Linq;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;

namespace DarkDescent.Tests
{
    public class SpecialEnemyMapTests
    {
        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static CaveSettings Caves => AssetDatabase.LoadAssetAtPath<CaveSettings>("Assets/_Project/Data/Levels/CaveSettings.asset");

        private static int CountSymbol(LevelMap map, char symbol)
        {
            return map.Markers.Count(m => m.Symbol == symbol);
        }

        private static ulong SeedWithUniqueAt(DungeonSettings settings, int depth)
        {
            for (ulong seed = 1; seed < 500; seed++)
            {
                if (settings.UniqueDepth(seed) == depth)
                {
                    return seed;
                }
            }

            Assert.Fail($"nessun seme con l'unico alla profondità {depth}");
            return 0;
        }

        [Test, Description("La mappa di un livello ha i campioni nella direttiva, su nemici veri, e l'unico solo alla profondità che dice il seme")]
        public void CreateMap_HasChampionsAndTheUnique()
        {
            foreach (var (settings, depth) in new[] { (Crypt, 3), (Caves, 6) })
            {
                ulong seed = SeedWithUniqueAt(settings, depth);
                var map = DungeonLevel.CreateMap(settings, seed, depth, out _);
                Assert.AreEqual(1, CountSymbol(map, SpecialEnemyPlanner.UniqueSymbol), settings.name);
                var champions = ChampionSpawn.FromMap(map);
                Assert.That(champions.Count, Is.InRange(1, settings.Specials.Groups + 1), settings.name);
                foreach (var champion in champions)
                {
                    Assert.IsTrue(SpecialEnemyPlanner.CanBeChampion(map.GetSymbol(champion.X, champion.Y)));
                    Assert.AreEqual(settings.Specials.ModifiersAt(depth), champion.Modifiers.Count);
                    foreach (var id in champion.Modifiers)
                    {
                        Assert.IsNotNull(settings.Champions.Find(id), id);
                    }
                }

                var other = DungeonLevel.CreateMap(settings, seed, depth + 1, out _);
                Assert.AreEqual(0, CountSymbol(other, SpecialEnemyPlanner.UniqueSymbol), "una profondità sola per mondo");
            }
        }

        [Test, Description("Alla prima profondità non ci sono né campioni né l'unico")]
        public void CreateMap_FirstDepth_HasNoSpecials()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var map = DungeonLevel.CreateMap(Crypt, seed, 1, out _);
                Assert.IsEmpty(map.GetDirective(ChampionSpawn.Directive));
                Assert.AreEqual(0, CountSymbol(map, SpecialEnemyPlanner.UniqueSymbol));
            }
        }

        [Test, Description("La cripta ha Morvak e le caverne Grumm, unici con il loro nome, due modificatori fissi e la tinta (D6)")]
        public void Tilesets_HaveTheirUnique()
        {
            foreach (var (tileset, name, nameKey) in new[]
            {
                ("DungeonTileset", "Morvak", "enemy.morvak"),
                ("CaveTileset", "Grumm", "enemy.grumm"),
            })
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelTileset>($"Assets/_Project/Data/Levels/{tileset}.asset");
                var prefab = asset.GetMarkerPrefab(SpecialEnemyPlanner.UniqueSymbol);
                Assert.IsNotNull(prefab, tileset);
                Assert.AreEqual(name, prefab.name);
                var special = prefab.GetComponent<SpecialEnemy>();
                Assert.AreEqual(EnemyRank.Unique, special.Rank);
                Assert.AreEqual(2, special.Modifiers.Count);
                Assert.AreNotEqual(UnityEngine.Color.white, special.Tint);
                Assert.AreEqual(nameKey, prefab.GetComponent<EnemyAI>().Archetype.NameKey);
            }
        }
    }
}

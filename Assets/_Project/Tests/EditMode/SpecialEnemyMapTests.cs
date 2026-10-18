using System.Linq;
using DarkDescent.Enemies;
using DarkDescent.Interaction;
using DarkDescent.Items;
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

        [Test, Description("La mappa della profondità del re ha il suo tesoro, una volta sola; le altre no")]
        public void CreateMap_KingsTreasureOnlyWithTheKing()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var map = DungeonLevel.CreateMap(Caves, seed, Caves.BossDepth, out var layout);
                Assert.AreEqual(layout.HasBossRoom ? 1 : 0, CountSymbol(map, SpecialEnemyPlanner.TreasureSymbol), $"seme {seed}");
                Assert.AreEqual(0, CountSymbol(DungeonLevel.CreateMap(Caves, seed, Caves.BossDepth - 1, out _), SpecialEnemyPlanner.TreasureSymbol));
                Assert.AreEqual(0, CountSymbol(DungeonLevel.CreateMap(Crypt, seed, 4, out _), SpecialEnemyPlanner.TreasureSymbol));
            }
        }

        [Test, Description("I tileset hanno forziere e tesoro del re: casse con il loro nome, il loro loot in più (D8) e il tesoro sigillato finché il re è vivo")]
        public void Tilesets_HaveGoldChestAndTreasure()
        {
            foreach (var tileset in new[] { "DungeonTileset", "CaveTileset" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelTileset>($"Assets/_Project/Data/Levels/{tileset}.asset");
                var gold = asset.GetMarkerPrefab(SpecialEnemyPlanner.GoldChestSymbol).GetComponent<Chest>();
                Assert.AreEqual("chest.gold", gold.GetComponent<Interactable>().LabelKey, tileset);
                Assert.AreEqual(1, gold.Bonus.ExtraRolls);
                Assert.AreEqual(Rarity.Magic, gold.Bonus.Guaranteed);
                Assert.AreEqual(3f, gold.Bonus.GoldFactor);
                Assert.IsFalse(gold.OpensWithBoss);

                var treasure = asset.GetMarkerPrefab(SpecialEnemyPlanner.TreasureSymbol).GetComponent<Chest>();
                Assert.AreEqual("chest.treasure", treasure.GetComponent<Interactable>().LabelKey, tileset);
                Assert.AreEqual(2, treasure.Bonus.ExtraRolls);
                Assert.AreEqual(Rarity.Rare, treasure.Bonus.Guaranteed);
                Assert.IsTrue(treasure.OpensWithBoss);
            }

            var chest = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Prefabs/Dungeon/Chest.prefab").GetComponent<Chest>();
            Assert.AreEqual(0, chest.Bonus.ExtraRolls, "la cassa di sempre resta com'era");
            Assert.IsFalse(chest.OpensWithBoss);
        }
    }
}

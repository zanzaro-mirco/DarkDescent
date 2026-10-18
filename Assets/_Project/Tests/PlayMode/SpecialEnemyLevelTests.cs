using System.Collections;
using System.Linq;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class SpecialEnemyLevelTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static DungeonSettings Crypt => AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/_Project/Data/Levels/CryptSettings.asset");

        private static IEnumerator Enter(int depth)
        {
            Manager.LoadLevel("Level_Crypt", "FromAbove", depth);
            float elapsed = 0f;
            while (elapsed < 15f)
            {
                if (!Manager.IsTransitioning && Manager.CurrentLevel != null && Manager.CurrentLevel.Depth == depth)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"la profondità {depth} non si è caricata");
        }

        [UnityTest, Description("Nel livello costruito i nemici della direttiva sono campioni con i loro modificatori, Morvak è l'unico, gli altri sono normali; tutti legati, con l'anello")]
        public IEnumerator BuiltLevel_HasChampionsAndMorvak()
        {
            yield return LoadGeneratedCore();
            ulong seed = 1;
            while (Crypt.UniqueDepth(seed) != 3)
            {
                seed++;
            }

            Manager.RunSeed = seed;
            yield return Enter(3);

            var level = Manager.CurrentLevel;
            var champions = ChampionSpawn.FromMap(level.Map);
            Assert.IsNotEmpty(champions);
            var specials = level.Enemies.Select(e => e.GetComponent<SpecialEnemy>()).Where(s => s != null).ToList();
            Assert.AreEqual(champions.Count, specials.Count(s => s.Rank == EnemyRank.Champion));
            foreach (var champion in champions)
            {
                var at = LevelMap.CellCenter(champion.X, champion.Y);
                var enemy = level.Enemies.OrderBy(e => Vector3.Distance(Flat(e.transform.position), Flat(at))).First();
                Assert.Less(Vector3.Distance(Flat(enemy.transform.position), Flat(at)), 0.5f, "il nemico della cella del campione");
                var special = enemy.GetComponent<SpecialEnemy>();
                Assert.AreEqual(EnemyRank.Champion, special.Rank, enemy.name);
                CollectionAssert.AreEqual(champion.Modifiers, special.Modifiers);
            }

            var unique = specials.Single(s => s.Rank == EnemyRank.Unique);
            StringAssert.StartsWith("Morvak", unique.name);

            // il composition root li ha legati: numeri del grado, modificatori risolti, anello (12.3)
            foreach (var special in specials)
            {
                Assert.IsTrue(special.IsBound, special.name);
                Assert.IsNotNull(special.Ring, special.name);
                Assert.AreEqual(special.Modifiers.Count, special.ActiveModifiers.Count, special.name);
            }

            Assert.AreEqual(5f, unique.Numbers.Life);
            Assert.AreEqual(level.Enemies.Count, level.Map.Markers.Count(m => CavePopulator.IsEnemy(m.Symbol)));
        }

        private static Vector3 Flat(Vector3 position)
        {
            return new Vector3(position.x, 0f, position.z);
        }
    }
}

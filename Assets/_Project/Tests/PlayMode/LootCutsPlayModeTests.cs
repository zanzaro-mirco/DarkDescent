using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Il loot misurato nei livelli veri (D13 della M13): scorte, campioni e casse vuote.</summary>
    public class LootCutsPlayModeTests : SandboxFixture
    {
        private static LevelManager Manager => Object.FindFirstObjectByType<LevelManager>();

        private static CompositionRoot Root => Object.FindFirstObjectByType<CompositionRoot>();

        private static IEnumerator Travel(int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel("Level_Crypt", "Start", depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, "la cripta non arriva");
                yield return null;
            }

            yield return null;
        }

        [UnityTest, Description("Una cassa senza oggetto dà al suo posto una pozione o il doppio dell'oro: con 120 semi escono tutte e due le cose")]
        public IEnumerator EmptyChest_GivesPotionOrMoreGold()
        {
            yield return LoadGeneratedCore();
            var chest = Manager.CurrentLevel.Chests.First(c => c.Bonus == null || c.Bonus.ExtraRolls == 0);
            int empty = 0;
            int withItem = 0;
            int potions = 0;
            int richGold = 0;
            var plainGold = new List<int>();
            for (ulong seed = 1; seed <= 120; seed++)
            {
                Root.UseLootSeed(seed);
                if (chest.Preview() != null)
                {
                    withItem++;
                    plainGold.Add(chest.PreviewGold());
                    continue;
                }

                empty++;
                if (chest.PreviewPotion() != null)
                {
                    potions++;
                }
                else
                {
                    Assert.GreaterOrEqual(chest.PreviewGold(), 2 * chest.LootTable.GoldRange.x, $"seme {seed}: l'oro è doppio");
                    richGold++;
                }
            }

            Assert.That(withItem / 120.0, Is.InRange(0.22, 0.48), "un oggetto una volta su tre");
            Assert.Greater(potions, 5);
            Assert.Greater(richGold, 5);
            Assert.AreEqual(empty, potions + richGold);
        }

        [UnityTest, Description("Nella cripta le scorte sono segnate e stanno vicine al loro capo; il campione non ha il tiro di sempre e lascia al più un oggetto")]
        public IEnumerator Escorts_AreMarkedAndChampionsDropOnce()
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel(3);
                if (Manager.CurrentLevel.Enemies.Any(e => e != null && e.GetComponent<LootDrop>().IsEscort))
                {
                    break;
                }
            }

            var enemies = Manager.CurrentLevel.Enemies.Where(e => e != null).ToList();
            var leaders = enemies.Where(e => e.TryGetComponent(out SpecialEnemy _)).ToList();
            var escorts = enemies.Where(e => e.GetComponent<LootDrop>().IsEscort).ToList();
            Assert.Greater(leaders.Count, 0, "un capo");
            Assert.Greater(escorts.Count, 0, "una scorta");
            foreach (var escort in escorts)
            {
                Assert.IsFalse(escort.TryGetComponent(out SpecialEnemy _), "una scorta non è un capo");
                Assert.IsTrue(leaders.Any(l => FlatDistance(l.transform.position, escort.transform.position) <= 3 * LevelMap.CellSize + 0.1f),
                    $"{escort.name} lontana da ogni capo");
            }

            var extra = new List<ItemInstance>();
            foreach (var leader in leaders.Where(l => l.GetComponent<SpecialEnemy>().Rank == EnemyRank.Champion))
            {
                var drop = leader.GetComponent<LootDrop>();
                for (ulong seed = 1; seed <= 20; seed++)
                {
                    Root.UseLootSeed(seed);
                    Assert.IsNull(drop.Preview(), "il campione non ha il tiro di sempre");
                    drop.PreviewExtra(extra);
                    Assert.LessOrEqual(extra.Count, 1);
                }
            }
        }
    }
}

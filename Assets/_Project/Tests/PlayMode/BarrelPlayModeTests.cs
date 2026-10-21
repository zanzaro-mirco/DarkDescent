using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>I barili da rompere nella cripta (D5 della M13).</summary>
    public class BarrelPlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentBarrels_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, _folder);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Environment.SetEnvironmentVariable(CompositionRoot.SaveFolderVariable, null);
            yield return null;
            Directory.Delete(_folder, true);
        }

        private static LevelManager Manager => UnityEngine.Object.FindFirstObjectByType<LevelManager>();

        private static CompositionRoot Root => UnityEngine.Object.FindFirstObjectByType<CompositionRoot>();

        private static string Describe(ItemInstance item)
        {
            return item == null ? "niente" : $"{item.Definition.name} {item.Rarity} L{item.ItemLevel}";
        }

        private static HashSet<GroundItem> GroundItems()
        {
            return new HashSet<GroundItem>(UnityEngine.Object.FindObjectsByType<GroundItem>(FindObjectsSortMode.None));
        }

        private static IEnumerator Travel(string scene, string entrance, int depth)
        {
            var before = Manager.CurrentLevel;
            Manager.LoadLevel(scene, entrance, depth);
            yield return null;
            for (float t = 0f; Manager.IsTransitioning || Manager.CurrentLevel == null || Manager.CurrentLevel == before; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, 15f, scene + " non arriva");
                yield return null;
            }

            yield return null;
            yield return null;
        }

        // la cripta al livello 1 del primo seme con dei barili: con il seme dell'orologio una stanza con i
        // barili non è sicura
        private IEnumerator GoToCryptWithBarrels()
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 10; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel("Level_Crypt", "Start", 1);
                if (Manager.CurrentLevel.Breakables.Count > 0)
                {
                    yield break;
                }
            }

            Assert.Fail("nessun seme da 1 a 10 dà dei barili al livello 1");
        }

        // gli scheletri non disturbano: il test parla del barile
        private static void ClearEnemies()
        {
            foreach (var enemy in Manager.CurrentLevel.Enemies)
            {
                if (enemy != null && enemy.TryGetComponent(out Health health) && !health.IsDead)
                {
                    health.TakeDamage(new DamageInfo(health.Max, DamageType.Physical, null));
                }
            }
        }

        // un punto del NavMesh a un paio di metri dal barile, da cui ci si arriva
        private static Vector3 NearbyStand(Breakable barrel)
        {
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                Vector3 probe = barrel.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (barrel.Radius + 1.6f);
                var path = new NavMeshPath();
                if (NavMesh.SamplePosition(probe, out var hit, 0.3f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(hit.position, barrel.transform.position + (hit.position - barrel.transform.position).normalized * (barrel.Radius + 0.5f), NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }

            Assert.Fail($"attorno a {barrel.name} non c'è posto");
            return Vector3.zero;
        }

        [UnityTest, Description("Il cerchio rosso segna il barile sotto il cursore; un click: il cavaliere lo colpisce, si rompe e il loot previsto dal seme cade dalla sua parte; rotto non si segna né si colpisce più")]
        public IEnumerator Click_BreaksTheBarrelAndDropsTheSeededLoot()
        {
            yield return GoToCryptWithBarrels();
            ClearEnemies();
            yield return new WaitForSeconds(0.5f);

            var level = Manager.CurrentLevel;
            var barrel = level.Breakables[0];
            Assert.AreEqual(LayerMask.NameToLayer("Enemy"), barrel.gameObject.layer);
            ItemInstance expected = null;
            for (ulong seed = 1; seed < 5000 && expected == null; seed++)
            {
                Root.UseLootSeed(seed);
                expected = barrel.Preview();
            }

            Assert.IsNotNull(expected, "nessun seme fa cadere un oggetto dal barile");
            int breaks = 0;
            barrel.Broken += _ => breaks++;

            PlayerAgent.Warp(NearbyStand(barrel));
            yield return new WaitForSeconds(0.5f);
            var marker = UnityEngine.Object.FindFirstObjectByType<TargetMarker>();
            PointAt(barrel.transform.position + Vector3.up * 0.8f);
            yield return null;
            yield return null;
            Assert.AreSame(barrel, marker.BarrelTarget, "il cerchio segna il barile sotto il cursore");
            Assert.IsNull(marker.Target);

            var before = GroundItems();
            ClickAt(barrel.transform.position + Vector3.up * 0.8f);
            for (float t = 0f; !barrel.IsBroken; t += Time.deltaTime)
            {
                Assert.Less(t, 8f, "il cavaliere non ha rotto il barile");
                yield return null;
            }

            Assert.AreEqual(1, breaks);
            Assert.IsNull(marker.BarrelTarget, "rotto, il cerchio si spegne");
            Assert.IsFalse(barrel.GetComponent<Collider>().enabled);
            Assert.IsFalse(barrel.GetComponent<NavMeshObstacle>().enabled, "il passaggio si apre");
            Assert.IsFalse(barrel.transform.Find("Intact").gameObject.activeSelf);
            Assert.IsTrue(barrel.transform.Find("Broken").gameObject.activeSelf, "i pezzi a terra");

            yield return new WaitForSeconds(1f);
            var dropped = GroundItems().Where(g => !before.Contains(g) && g.Gold == 0 && !(g.Item.Definition is PotionDefinition)).ToList();
            Assert.AreEqual(1, dropped.Count, "un oggetto dal barile");
            Assert.AreEqual(Describe(expected), Describe(dropped[0].Item), "l'oggetto previsto dal seme");
            Assert.Less(FlatDistance(dropped[0].transform.position, Player.position), FlatDistance(barrel.transform.position, Player.position) + 0.5f,
                "cade dalla parte del cavaliere, non dietro il barile");

            PointAt(Vector3.zero);
            yield return null;
            PointAt(barrel.transform.position + Vector3.up * 0.3f);
            yield return null;
            yield return null;
            Assert.IsNull(marker.BarrelTarget, "rotto non si segna più");
            barrel.TakeDamage(new DamageInfo(10f, DamageType.Physical, Player.gameObject));
            Assert.AreEqual(1, breaks, "si rompe una volta sola");
        }

        [UnityTest, Description("Un barile rotto resta rotto tornando dalla città, senza lasciare di nuovo il suo loot; gli altri restano interi")]
        public IEnumerator BrokenBarrel_StaysBrokenAfterLeaving()
        {
            yield return GoToCryptWithBarrels();
            var level = Manager.CurrentLevel;
            int count = level.Breakables.Count;
            int index = count - 1;
            ulong runSeed = Manager.RunSeed;

            // il primo seme del loot con cui il barile lascia qualcosa: al ritorno non deve raddoppiare
            for (ulong seed = 1; seed < 5000; seed++)
            {
                Root.UseLootSeed(seed);
                if (level.Breakables[index].Preview() != null || level.Breakables[index].PreviewGold() > 0)
                {
                    break;
                }
            }

            level.Breakables[index].Break(Player.gameObject);
            yield return new WaitForSeconds(1f);
            int ground = GroundItems().Count(g => g.gameObject.scene == level.gameObject.scene);
            Assert.Greater(ground, 0);

            yield return Travel("Town", "Start", 0);
            Manager.RunSeed = runSeed;
            yield return Travel("Level_Crypt", "Start", 1);
            level = Manager.CurrentLevel;
            Assert.AreEqual(count, level.Breakables.Count, "lo stesso livello dal seme");
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(i == index, level.Breakables[i].IsBroken, $"barile {i}");
            }

            Assert.IsFalse(level.Breakables[index].GetComponent<Collider>().enabled);
            Assert.IsTrue(level.Breakables[index].transform.Find("Broken").gameObject.activeSelf);
            yield return null;
            Assert.AreEqual(ground, GroundItems().Count(g => g.gameObject.scene == level.gameObject.scene && g.gameObject.activeInHierarchy),
                "il loot è quello di prima, non uno nuovo");
        }
    }
}

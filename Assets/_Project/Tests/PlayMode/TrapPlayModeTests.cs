using System;
using System.Collections;
using System.IO;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>Spuntoni e barili esplosivi (D8 della M13).</summary>
    public class TrapPlayModeTests : SandboxFixture
    {
        private string _folder;

        [SetUp]
        public void UseTemporarySaves()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DarkDescentTraps_" + Guid.NewGuid().ToString("N"));
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

        private Health PlayerHealth => Player.GetComponent<Health>();

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
            yield return null;
        }

        // la cripta alla profondità data del primo seme che ha quello che serve; i nemici fermi
        private IEnumerator GoToCrypt(int depth, Func<LevelContext, bool> wanted)
        {
            yield return LoadGeneratedCore();
            for (ulong seed = 1; seed <= 40; seed++)
            {
                Manager.RunSeed = seed;
                yield return Travel(depth);
                if (wanted(Manager.CurrentLevel))
                {
                    foreach (var enemy in Manager.CurrentLevel.Enemies)
                    {
                        if (enemy != null)
                        {
                            enemy.enabled = false;
                        }
                    }

                    yield break;
                }
            }

            Assert.Fail($"nessun seme da 1 a 40 ha quello che serve alla profondità {depth}");
        }

        private static Vector3 OnNavMesh(Vector3 near)
        {
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 1.5f, NavMesh.AllAreas), $"niente NavMesh vicino a {near}");
            return hit.position;
        }

        // un punto del NavMesh attorno al barile entro la distanza, il primo girando dall'angolo dato
        private static Vector3 Around(Vector3 center, float from, float within)
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = (from + i * 11.25f) * Mathf.Deg2Rad;
                Vector3 probe = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.8f;
                if (NavMesh.SamplePosition(probe, out var hit, 0.6f, NavMesh.AllAreas) && FlatDistance(hit.position, center) < within)
                {
                    return hit.position;
                }
            }

            Assert.Fail($"attorno a {center} non c'è posto");
            return center;
        }

        [UnityTest, Description("Gli spuntoni feriscono il cavaliere che ci sta sopra quando escono, una volta per scatto e per la parte della vita della profondità; fuori dalla piastrella niente, e un nemico sopra non si fa male")]
        public IEnumerator Spikes_HurtOnlyTheKnight()
        {
            yield return GoToCrypt(3, level => level.Traps.Count > 0);
            var trap = Manager.CurrentLevel.Traps[0];
            var enemy = Manager.CurrentLevel.Enemies.First(e => e != null);
            var enemyHealth = enemy.GetComponent<Health>();
            enemy.GetComponent<NavMeshAgent>().Warp(OnNavMesh(trap.transform.position + Vector3.right * 0.6f));
            float enemyLife = enemyHealth.Current;

            PlayerAgent.Warp(OnNavMesh(trap.transform.position));
            PlayerHealth.SetCurrent(PlayerHealth.Max);
            int strikes = 0;
            trap.Struck += _ => strikes++;
            trap.SetTime(0f);
            yield return null;
            Assert.AreEqual(SpikeTrap.SpikePhase.Rest, trap.Phase);
            Assert.AreEqual(PlayerHealth.Max, PlayerHealth.Current, 0.01f, "a riposo non ferisce");

            for (float t = 0f; trap.Phase != SpikeTrap.SpikePhase.Out; t += Time.deltaTime)
            {
                Assert.Less(t, 4f, "gli spuntoni non escono");
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, strikes, "uno scatto, una ferita");
            Assert.AreEqual(PlayerHealth.Max * (1f - TrapRules.SpikeFraction(3)), PlayerHealth.Current, 0.5f);
            Assert.AreEqual(enemyLife, enemyHealth.Current, 0.01f, "il nemico non si fa male");

            // fuori dalla piastrella, il giro dopo niente
            PlayerAgent.Warp(OnNavMesh(trap.transform.position + Vector3.forward * 4f));
            float life = PlayerHealth.Current;
            yield return new WaitForSeconds(trap.Cycle + 0.2f);
            Assert.AreEqual(1, strikes);
            Assert.AreEqual(life, PlayerHealth.Current, 0.01f);
        }

        [UnityTest, Description("Il barile esplosivo rotto accende la miccia, dopo un secondo esplode: ferisce il cavaliere e i nemici nel raggio e rompe i barili vicini, chi è lontano no")]
        public IEnumerator ExplosiveBarrel_HurtsEveryoneNearby()
        {
            yield return GoToCrypt(4, level => level.Breakables.Any(b => b.IsExplosive));
            var level = Manager.CurrentLevel;
            var barrel = level.Breakables.First(b => b.IsExplosive);
            Assert.IsTrue(barrel.transform.Find("Explosive").gameObject.activeSelf, "si vede il barile esplosivo");
            Assert.IsFalse(barrel.transform.Find("Intact").gameObject.activeSelf);

            var enemies = level.Enemies.Where(e => e != null).Take(2).ToList();
            Assert.AreEqual(2, enemies.Count);
            Vector3 center = barrel.transform.position;
            enemies[0].GetComponent<NavMeshAgent>().Warp(Around(center, 0f, 2.5f));
            PlayerAgent.Warp(Around(center, 180f, 2.5f));
            enemies[1].GetComponent<NavMeshAgent>().Warp(Player.position + (Player.position - center).normalized * 6f);
            yield return null;
            Assert.Less(FlatDistance(enemies[0].transform.position, center), 2.6f);
            Assert.Greater(FlatDistance(enemies[1].transform.position, center), 4f);
            PlayerHealth.SetCurrent(PlayerHealth.Max);
            var nearHealth = enemies[0].GetComponent<Health>();
            var farHealth = enemies[1].GetComponent<Health>();
            float nearLife = nearHealth.Current;
            float farLife = farHealth.Current;
            var neighbours = level.Breakables.Where(b => b != barrel && Vector3.Distance(b.transform.position, center) < 2.5f).ToList();
            var distant = level.Breakables.Where(b => Vector3.Distance(b.transform.position, center) > 9f).ToList();
            bool exploded = false;
            barrel.Exploded += _ => exploded = true;

            barrel.TakeDamage(new DamageInfo(5f, DamageType.Physical, Player.gameObject));
            Assert.IsTrue(barrel.IsBroken);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(exploded, "la miccia dura un secondo");
            Assert.AreEqual(PlayerHealth.Max, PlayerHealth.Current, 0.01f);
            yield return new WaitForSeconds(0.8f);

            Assert.IsTrue(exploded);
            float damage = TrapRules.ExplosionDamage(4);
            Assert.AreEqual(PlayerHealth.Max - damage, PlayerHealth.Current, 0.5f, "il cavaliere nel raggio");
            Assert.AreEqual(Mathf.Max(0f, nearLife - damage), nearHealth.Current, 0.5f, "il nemico vicino");
            Assert.AreEqual(farLife, farHealth.Current, 0.01f, "il nemico lontano no");
            Assert.IsTrue(neighbours.All(b => b.IsBroken), "i barili vicini si rompono");
            Assert.IsTrue(distant.All(b => !b.IsBroken), "quelli lontani no");
            Assert.IsTrue(barrel.transform.Find("Broken").gameObject.activeSelf);
            Assert.IsFalse(barrel.transform.Find("Explosive").gameObject.activeSelf);
        }
    }
}

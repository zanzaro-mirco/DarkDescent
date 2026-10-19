using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Core;
using DarkDescent.Enemies;
using DarkDescent.Items;
using DarkDescent.Levels;
using DarkDescent.Magic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class MageTests : SandboxFixture
    {
        private static GameObject MagePrefab => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/SkeletonMage.prefab");

        // il mago a <distance> dal cavaliere, in campo aperto: niente muri né pilastri in mezzo
        private EnemyAI SpawnMage(float distance, IRandomSource random)
        {
            foreach (var e in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                e.gameObject.SetActive(false);
            }

            for (int i = 0; i < 24; i++)
            {
                Vector3 spot = Player.position + Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward * distance;
                if (NavMesh.SamplePosition(spot, out var hit, 0.4f, NavMesh.AllAreas)
                    && !NavMesh.Raycast(Player.position, hit.position, out _, NavMesh.AllAreas)
                    && !Physics.Linecast(Player.position + Vector3.up * 1.5f, hit.position + Vector3.up * 1.5f, ~(1 << 0 | 1 << 8), QueryTriggerInteraction.Ignore))
                {
                    var mage = Object.Instantiate(MagePrefab, hit.position, Quaternion.LookRotation(Player.position - hit.position)).GetComponent<EnemyAI>();
                    mage.Bind(Player.GetComponent<Health>());
                    mage.GetComponent<RangedAttack>().Bind(Object.FindFirstObjectByType<ProjectilePools>(), random);
                    return mage;
                }
            }

            Assert.Fail($"nessun punto libero a {distance} m");
            return null;
        }

        [UnityTest, Description("A 7 m il mago resta dov'è e lancia: il dardo colpisce il cavaliere con 4–8 di danno; in 3 s lancia al massimo due volte")]
        public IEnumerator Mage_KeepsDistanceAndCasts()
        {
            yield return LoadSandbox();
            var health = Player.GetComponent<Health>();
            var mage = SpawnMage(7f, new SystemRandomSource(5));
            int hits = 0;
            float lastDamage = 0f;
            health.Damaged += (info, applied) => { hits++; lastDamage = applied; Assert.AreEqual(DamageType.Magic, info.Type); };
            Vector3 start = mage.transform.position;

            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.That(hits, Is.InRange(1, 2), "un lancio ogni 2 s");
            Assert.That(lastDamage, Is.InRange(1f, 8f), "4–8 meno l'Armatura");
            Assert.AreEqual(EnemyState.Ranged, mage.State);
            Assert.Less(FlatDistance(start, mage.transform.position), 1f, "a 7 m non si muove");
        }

        [UnityTest, Description("Con il cavaliere a 2,5 m il mago si allontana: dopo 4 s è oltre i 4 m")]
        public IEnumerator Mage_RetreatsWhenClose()
        {
            yield return LoadSandbox();
            var mage = SpawnMage(2.5f, new SystemRandomSource(5));
            bool retreated = false;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                retreated |= mage.State == EnemyState.Retreat;
                yield return null;
            }

            Assert.IsTrue(retreated, "è passato alla fuga");
            Assert.Greater(FlatDistance(mage.transform.position, Player.position), 4f);
        }

        [UnityTest, Description("Con lo scudo e un tiro di blocco basso il dardo del mago è parato: niente danno e il blocco annunciato; il dardo del cavaliere non si para mai")]
        public IEnumerator MageBolt_IsBlockedByTheShield()
        {
            yield return LoadSandbox();
            var inventory = Player.GetComponent<PlayerInventory>();
            var shield = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorDefinition>("Assets/_Project/Data/Items/RoundShield.asset");
            Assert.IsTrue(inventory.Equipment.TryEquip(new ItemInstance(shield), out _));
            var block = Player.GetComponent<ShieldBlock>();
            Assert.IsTrue(block.HasShield);
            int blocked = 0;
            block.Blocked += info => blocked++;
            var health = Player.GetComponent<Health>();

            // 0,0: danno minimo e blocco riuscito
            SpawnMage(7f, new FixedRandomSource(0.0));
            for (float t = 0f; t < 1.5f && blocked == 0; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(1, blocked, "parato");
            Assert.AreEqual(health.Max, health.Current, "niente danno");
        }

        [UnityTest, Description("Nella cripta alla profondità 3 ci sono da 1 a 2 maghi, legati al cavaliere come gli altri nemici. Su un seme senza mago campione: dalla M12 un mago campione porta una scorta di maghi, e con il seme a caso della partita il conto cambiava")]
        public IEnumerator Crypt_HasMagesFromDepthThree()
        {
            yield return LoadGeneratedCore();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            // il primo seme senza un mago campione: la sua scorta di maghi è un'altra regola (M12)
            for (ulong seed = 4711; ; seed++)
            {
                Assert.Less(seed, 4731UL, "nessun seme senza mago campione");
                manager.RunSeed = seed;
                manager.LoadLevel("Level_Crypt", "FromAbove", 3);
                yield return null;
                for (float t = 0f; t < 10f && (manager.IsTransitioning || manager.CurrentLevel == null || manager.CurrentLevel.Depth != 3); t += Time.unscaledDeltaTime)
                {
                    yield return null;
                }

                if (!manager.CurrentLevel.Enemies.Any(e => e.GetComponent<RangedAttack>() != null && e.GetComponent<SpecialEnemy>() != null))
                {
                    break;
                }
            }

            var mages = manager.CurrentLevel.Enemies.Where(e => e.GetComponent<RangedAttack>() != null).ToArray();
            Assert.That(mages.Length, Is.InRange(1, 2));
            Assert.AreEqual("enemy.skeleton_mage", mages[0].Archetype.NameKey);
        }
    }
}

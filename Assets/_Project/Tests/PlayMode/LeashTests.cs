using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    /// <summary>
    /// Dalla seconda prova della build M7: i nemici inseguivano il cavaliere per tutto il livello. Oltre
    /// una distanza da casa lo lasciano, tornano dove li ha messi il livello e guariscono.
    /// </summary>
    public class LeashTests : SandboxFixture
    {
        [UnityTest, Description("Un nemico che insegue il cavaliere lontano si ferma al suo limite da casa (20 m, lo sciame 30), ci torna e riprende la vita piena")]
        public IEnumerator Enemy_StopsChasingFarFromHome()
        {
            yield return LoadGeneratedCore();
            var level = Object.FindFirstObjectByType<LevelManager>().CurrentLevel;

            // il nemico più lontano dal cavaliere, fermo all'ingresso; gli altri si spengono
            EnemyAI chaser = null;
            float farthest = 0f;
            foreach (var enemy in level.Enemies)
            {
                float distance = FlatDistance(enemy.transform.position, Player.position);
                if (distance > farthest)
                {
                    farthest = distance;
                    chaser = enemy;
                }
            }

            foreach (var enemy in level.Enemies)
            {
                enemy.enabled = enemy == chaser;
            }

            Assert.Greater(farthest, chaser.Archetype.LeashRange + 3f, "serve un nemico lontano dal cavaliere");
            float leash = chaser.Archetype.LeashRange;
            Vector3 home = chaser.HomePosition;
            var health = chaser.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(Mathf.Round(health.Max * 0.5f), DamageType.Physical, null));

            // un compagno l'ha avvisato: insegue senza averlo visto
            chaser.Alert();
            Assert.AreEqual(EnemyState.Chase, chaser.State);

            float farthestFromHome = 0f;
            for (float time = 0f; chaser.State != EnemyState.Return; time += Time.deltaTime)
            {
                Assert.Less(time, 15f, "non si è mai fermato");
                farthestFromHome = Mathf.Max(farthestFromHome, FlatDistance(home, chaser.transform.position));
                yield return null;
            }

            Assert.Less(farthestFromHome, leash + 0.5f, "si ferma al limite");
            Assert.IsFalse(chaser.GetComponent<MeleeAttack>().HasTarget, "lascia il cavaliere");

            for (float time = 0f; chaser.State != EnemyState.Idle; time += Time.deltaTime)
            {
                Assert.Less(time, 15f, "non è tornato a casa");
                Assert.AreEqual(EnemyState.Return, chaser.State, "tornando non riprende a inseguire");
                yield return null;
            }

            Assert.Less(FlatDistance(home, chaser.transform.position), 0.6f, "è a casa");
            Assert.AreEqual(health.Max, health.Current, "e ha la vita piena");
        }
    }
}

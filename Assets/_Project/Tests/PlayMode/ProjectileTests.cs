using System.Collections;
using System.Linq;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using DarkDescent.Magic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class ProjectileTests : SandboxFixture
    {
        private static SpellDefinition Firebolt => UnityEditor.AssetDatabase.LoadAssetAtPath<SpellDefinition>("Assets/_Project/Data/Spells/Firebolt.asset");

        // il cavaliere a <distance> dal nemico, con la linea libera sul NavMesh
        private void StandFacing(Transform enemy, float distance)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward * distance;
                if (NavMesh.SamplePosition(enemy.position + offset, out var hit, 0.5f, NavMesh.AllAreas)
                    && !NavMesh.Raycast(hit.position, enemy.position, out _, NavMesh.AllAreas))
                {
                    PlayerAgent.Warp(hit.position);
                    return;
                }
            }

            Assert.Fail("nessun punto libero attorno al nemico");
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime)
            {
                yield return null;
            }
        }

        [UnityTest, Description("Il Dardo parte dopo il gesto, colpisce lo scheletro con 9–15 di danno (Magia 10), lo sveglia, esplode e torna al pool")]
        public IEnumerator Firebolt_HitsEnemyAndReturnsToPool()
        {
            yield return LoadSandbox();
            var skeleton = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).First();
            var health = skeleton.GetComponent<Health>();
            StandFacing(skeleton.transform, 5f);
            yield return null;
            var pools = Object.FindFirstObjectByType<ProjectilePools>();
            var magic = Player.GetComponent<PlayerMagic>();

            Assert.AreEqual(CastResult.Ready, magic.TryCastAt(skeleton.transform.position));
            Assert.AreEqual(0, pools.ActiveCount, "il proiettile aspetta la mano");
            yield return WaitUntil(() => pools.ActiveCount > 0, 1f);
            Assert.AreEqual(1, pools.ActiveCount, "partito");

            yield return WaitUntil(() => health.Current < health.Max, 2f);
            float damage = health.Max - health.Current;
            Assert.That(damage, Is.InRange(9f, 15f), "6–12 + Magia 10 / 4");
            Assert.AreNotEqual(EnemyState.Idle, skeleton.State, "colpito da lontano, si sveglia");

            yield return WaitUntil(() => pools.ActiveCount == 0, 2f);
            Assert.AreEqual(0, pools.ActiveCount, "esploso e tornato nel pool");
            Assert.AreEqual(1, pools.CreatedCount(Firebolt.Effect));
        }

        [UnityTest, Description("Tirato contro un muro della cripta il Dardo si ferma lì, anche se il muro è dalla parte della camera e spento, e non colpisce nessuno")]
        public IEnumerator Firebolt_StopsOnWall()
        {
            yield return LoadGeneratedCore();
            foreach (var enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                enemy.gameObject.SetActive(false);
            }

            var pools = Object.FindFirstObjectByType<ProjectilePools>();

            // un punto con un muro tra 2 e 8 m in una delle sedici direzioni: dall'ingresso, o se la
            // cripta di questo seme non ce l'ha, da un vertice del NavMesh
            Vector3 origin = Player.position;
            Vector3 wall = Vector3.zero;
            Vector3 aim = Vector3.zero;
            var origins = new[] { origin }.Concat(NavMesh.CalculateTriangulation().vertices.Where((v, k) => k % 7 == 0));
            foreach (var from in origins)
            {
                if (!NavMesh.SamplePosition(from, out var start, 0.5f, NavMesh.AllAreas))
                {
                    continue;
                }

                for (int i = 0; i < 16 && aim == Vector3.zero; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                    // il bordo del NavMesh deve essere un muro: un arredo basso lo ritaglia, ma il
                    // dardo gli passa sopra e si ferma più avanti (passo 15.1)
                    if (NavMesh.Raycast(start.position, start.position + direction * 8f, out var hit, NavMesh.AllAreas) && hit.distance > 2f
                        && WallAhead(start.position, direction, hit.distance))
                    {
                        origin = start.position;
                        wall = hit.position;
                        aim = direction;
                    }
                }

                if (aim != Vector3.zero)
                {
                    break;
                }
            }

            Assert.AreNotEqual(Vector3.zero, aim, "un muro tra 2 e 8 m");
            PlayerAgent.Warp(origin);
            yield return null;
            origin = Player.position;
            var projectile = pools.Launch(Firebolt.Effect, origin + Vector3.up * 1.1f, aim, Firebolt.Speed, Firebolt.Range, 10f, Player.gameObject, ~0);
            yield return WaitUntil(() => projectile.IsExploding, 2f);
            Assert.IsTrue(projectile.IsExploding);
            Vector3 flat = projectile.transform.position - wall;
            flat.y = 0f;
            Assert.Less(flat.magnitude, 0.6f, "esploso sul muro, non oltre");
        }

        // il primo collider a mezzo metro d'altezza è un muro del livello, non un arredo basso che
        // ritaglia il NavMesh e che il dardo sorvola
        private static bool WallAhead(Vector3 from, Vector3 direction, float distance)
        {
            if (!Physics.Raycast(from + Vector3.up * 0.5f, direction, out var hit, distance + 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            for (var t = hit.collider.transform; t != null; t = t.parent)
            {
                if (t.name == "Walls")
                {
                    return true;
                }
            }

            return false;
        }

        [UnityTest, Description("Il pool riusa i proiettili: dieci lanci, poi altri dieci dopo l'esplosione, ne creano dieci; trenta lanci di fila dal pool caldo non allocano memoria gestita")]
        public IEnumerator Pool_ReusesWithoutAllocating()
        {
            yield return LoadSandbox();
            var pools = Object.FindFirstObjectByType<ProjectilePools>();
            var prefab = Firebolt.Effect;
            Vector3 from = Player.position + Vector3.up * 1.1f;
            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < 10; i++)
                {
                    pools.Launch(prefab, from, Quaternion.Euler(0f, i * 36f, 0f) * Vector3.forward, 14f, 1f, 1f, Player.gameObject, 0);
                }

                yield return WaitUntil(() => pools.ActiveCount == 0, 3f);
            }

            Assert.AreEqual(10, pools.CreatedCount(prefab), "la seconda raffica riusa la prima");

            // il pool tiene al massimo 32 proiettili: 30 alla volta, tre volte, restano tutti nel pool
            for (int i = 0; i < 30; i++)
            {
                pools.Launch(prefab, from, Vector3.forward, 14f, 1f, 1f, Player.gameObject, 0);
            }

            yield return WaitUntil(() => pools.ActiveCount == 0, 3f);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 30; i++)
            {
                pools.Launch(prefab, from, Vector3.forward, 14f, 1f, 1f, Player.gameObject, 0);
            }

            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0, allocated, "lanci dal pool caldo senza allocazioni");
        }
    }
}

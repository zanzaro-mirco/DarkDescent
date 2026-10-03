using System.Collections;
using DarkDescent.Combat;
using DarkDescent.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class EnemyAITests : SandboxFixture
    {
        // scheletro in (6, 0, 6): da (6, 0, 1) lo vede senza ostacoli in mezzo, a 5 m
        private static readonly Vector3 InSightPoint = new Vector3(6f, 0f, 1f);

        // da (2, 0, 2) la diagonale verso lo scheletro passa dentro il cubo in (4, 1, 3)
        private static readonly Vector3 BehindCubePoint = new Vector3(2f, 0f, 2f);

        private EnemyAI _ai;
        private MeleeAttack _skeletonAttack;
        private Health _skeletonHealth;
        private Health _playerHealth;

        private IEnumerator LoadArena()
        {
            yield return LoadSandbox();
            var skeleton = GameObject.Find("Skeleton");
            _ai = skeleton.GetComponent<EnemyAI>();
            _skeletonAttack = skeleton.GetComponent<MeleeAttack>();
            _skeletonHealth = skeleton.GetComponent<Health>();
            _playerHealth = Player.GetComponent<Health>();
        }

        private IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && !condition())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator WalkInSightAndWaitForSwing()
        {
            ClickAt(InSightPoint);
            yield return WaitFor(() => _skeletonAttack.IsSwinging, 8f);
            Assert.IsTrue(_skeletonAttack.IsSwinging, $"lo scheletro deve attaccare, stato {_ai.State}");
        }

        private static DamageInfo Damage(float amount)
        {
            return new DamageInfo(amount, DamageType.Physical, null);
        }

        [UnityTest, Description("La stanza della build ha tre scheletri, tutti sul NavMesh e fermi finché il player è lontano")]
        public IEnumerator Sandbox_HasThreeIdleSkeletonsOnNavMesh()
        {
            yield return LoadSandbox(allSkeletons: true);
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            Assert.AreEqual(3, enemies.Length);

            yield return new WaitForSeconds(1f);
            foreach (var enemy in enemies)
            {
                Assert.IsTrue(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, $"{enemy.name} deve stare sul NavMesh");
                Assert.AreEqual(EnemyState.Idle, enemy.State, $"{enemy.name} non deve vedere il player all'avvio");
                Assert.Greater(FlatDistance(enemy.transform.position, Player.position), 8f, $"{enemy.name} deve partire fuori dal raggio di aggro");
            }
        }

        [UnityTest, Description("Con il player lontano lo scheletro resta fermo in Idle")]
        public IEnumerator PlayerFar_SkeletonStaysIdle()
        {
            yield return LoadArena();
            var start = _ai.transform.position;

            yield return new WaitForSeconds(1f);

            Assert.AreEqual(EnemyState.Idle, _ai.State);
            Assert.Less(Vector3.Distance(start, _ai.transform.position), 0.01f);
        }

        [UnityTest, Description("Il player entra nel raggio di aggro: lo scheletro lo insegue, attacca e gli toglie vita")]
        public IEnumerator PlayerInSight_SkeletonChasesAndHits()
        {
            yield return LoadArena();
            float damage = _skeletonAttack.Weapon.Damage;

            ClickAt(InSightPoint);
            yield return WaitFor(() => _ai.State == EnemyState.Chase, 4f);
            Assert.AreEqual(EnemyState.Chase, _ai.State, "vedendo il player deve inseguirlo");

            yield return WaitFor(() => _playerHealth.Current < _playerHealth.Max, 8f);
            Assert.AreEqual(_playerHealth.Max - damage, _playerHealth.Current, 0.001f, "il colpo dello scheletro deve arrivare");
            Assert.AreEqual(EnemyState.Attack, _ai.State);
        }

        [UnityTest, Description("Dietro un ostacolo il player non viene notato, anche se è nel raggio")]
        public IEnumerator PlayerBehindObstacle_SkeletonStaysIdle()
        {
            yield return LoadArena();

            ClickAt(BehindCubePoint);
            yield return WaitFor(() => FlatDistance(Player.position, BehindCubePoint) < 0.3f, 5f);
            Assert.Less(FlatDistance(Player.position, _ai.transform.position), 8f, "il player deve essere nel raggio");

            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(EnemyState.Idle, _ai.State, "con il cubo in mezzo non deve vederlo");
        }

        [UnityTest, Description("Un colpo forte durante il fendente lo interrompe: il danno al player non arriva")]
        public IEnumerator StrongHitDuringSwing_InterruptsIt()
        {
            yield return LoadArena();
            yield return WalkInSightAndWaitForSwing();
            float before = _playerHealth.Current;

            // 10 su 30 supera la soglia del 20%
            _skeletonHealth.TakeDamage(Damage(10f));
            Assert.IsFalse(_skeletonAttack.IsSwinging, "il colpo deve essere annullato");
            Assert.IsTrue(_skeletonAttack.IsInterrupted);

            yield return new WaitForSeconds(_skeletonAttack.Weapon.HitDelay + 0.2f);
            Assert.AreEqual(before, _playerHealth.Current, "il fendente interrotto non deve fare danno");
        }

        [UnityTest, Description("Un colpo debole durante il fendente non lo interrompe")]
        public IEnumerator WeakHitDuringSwing_DoesNotInterrupt()
        {
            yield return LoadArena();
            yield return WalkInSightAndWaitForSwing();
            float before = _playerHealth.Current;

            // 3 su 30 resta sotto la soglia del 20%
            _skeletonHealth.TakeDamage(Damage(3f));
            Assert.IsTrue(_skeletonAttack.IsSwinging, "un colpo debole non interrompe");

            yield return new WaitForSeconds(_skeletonAttack.Weapon.HitDelay + 0.2f);
            Assert.Less(_playerHealth.Current, before, "il fendente deve arrivare");
        }

        [UnityTest, Description("Morto, lo scheletro smette di attaccare e non blocca più click né agent")]
        public IEnumerator SkeletonDies_StopsEverything()
        {
            yield return LoadArena();
            yield return WalkInSightAndWaitForSwing();

            _skeletonHealth.TakeDamage(Damage(_skeletonHealth.Max));
            float after = _playerHealth.Current;

            Assert.AreEqual(EnemyState.Dead, _ai.State);
            Assert.IsFalse(_ai.GetComponent<Collider>().enabled, "il collider va spento");
            Assert.IsFalse(_ai.GetComponent<NavMeshAgent>().enabled, "l'agent va spento");

            yield return new WaitForSeconds(_skeletonAttack.Weapon.AttackInterval + 0.5f);
            Assert.AreEqual(after, _playerHealth.Current, "un morto non colpisce, nemmeno con il fendente già partito");
        }

        [UnityTest, Description("Morto il player, lo scheletro torna in Idle e si ferma")]
        public IEnumerator PlayerDies_SkeletonReturnsToIdle()
        {
            yield return LoadArena();
            ClickAt(InSightPoint);
            yield return WaitFor(() => _ai.State == EnemyState.Chase, 4f);

            _playerHealth.TakeDamage(Damage(_playerHealth.Max));
            yield return null;
            yield return null;

            Assert.AreEqual(EnemyState.Idle, _ai.State);
            Assert.IsFalse(_skeletonAttack.HasTarget);
        }
    }
}

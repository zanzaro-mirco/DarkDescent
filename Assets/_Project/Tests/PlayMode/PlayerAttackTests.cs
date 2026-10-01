using System.Collections;
using DarkDescent.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DarkDescent.Tests
{
    public class PlayerAttackTests : SandboxFixture
    {
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeathHash = Animator.StringToHash("Death");

        private Transform _skeleton;
        private Health _skeletonHealth;
        private NavMeshAgent _skeletonAgent;
        private MeleeAttack _playerAttack;

        private IEnumerator LoadArena()
        {
            yield return LoadSandbox();
            _skeleton = GameObject.Find("Skeleton").transform;
            _skeletonHealth = _skeleton.GetComponent<Health>();
            _skeletonAgent = _skeleton.GetComponent<NavMeshAgent>();
            _playerAttack = Player.GetComponent<MeleeAttack>();
        }

        // centro del busto: il click deve cadere sul collider, non sul pavimento ai suoi piedi
        private Vector3 SkeletonAimPoint => _skeleton.position + Vector3.up;

        private float EdgeDistance()
        {
            return FlatDistance(Player.position, _skeleton.position) - PlayerAgent.radius - _skeletonAgent.radius;
        }

        private static int StateOf(Transform character)
        {
            return character.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).shortNameHash;
        }

        private IEnumerator WaitUntilDamaged(float timeout)
        {
            float elapsed = 0f;
            while (elapsed < timeout && _skeletonHealth.Current >= _skeletonHealth.Max)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        [UnityTest, Description("Click su uno scheletro: il cavaliere si avvicina, colpisce una volta sola e lo scheletro reagisce")]
        public IEnumerator ClickOnSkeleton_ApproachesAndHitsOnce()
        {
            yield return LoadArena();
            float damage = _playerAttack.Weapon.Damage;

            ClickAt(SkeletonAimPoint);
            yield return WaitUntilDamaged(6f);

            Assert.AreEqual(_skeletonHealth.Max - damage, _skeletonHealth.Current, 0.001f, "il colpo deve arrivare");
            Assert.LessOrEqual(EdgeDistance(), _playerAttack.Weapon.Range + 0.05f, "il colpo deve partire a portata");

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(HitHash, StateOf(_skeleton), "lo scheletro deve reagire al colpo");

            // un click = un colpo: dopo un altro intervallo d'attacco la vita è la stessa
            yield return new WaitForSeconds(_playerAttack.Weapon.AttackInterval + 0.5f);
            Assert.AreEqual(_skeletonHealth.Max - damage, _skeletonHealth.Current, 0.001f, "un solo click deve dare un solo colpo");
        }

        [UnityTest, Description("Tenendo premuto su uno scheletro lo si colpisce finché muore, poi ci si ferma")]
        public IEnumerator HoldOnSkeleton_KeepsAttackingUntilDead()
        {
            yield return LoadArena();

            PointAt(SkeletonAimPoint);
            Press(Mouse.leftButton);

            float elapsed = 0f;
            while (elapsed < 10f && !_skeletonHealth.IsDead)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(_skeletonHealth.IsDead, $"lo scheletro deve morire, vita {_skeletonHealth.Current}");

            yield return new WaitForSeconds(_playerAttack.Weapon.AttackInterval + 0.5f);
            Assert.AreEqual(DeathHash, StateOf(_skeleton), "lo scheletro deve restare in Death");
            Assert.IsFalse(_playerAttack.HasTarget, "morto il bersaglio, l'attacco lo lascia");
            Assert.IsFalse(PlayerAgent.hasPath, "tenendo premuto su un morto il cavaliere resta fermo");
            Release(Mouse.leftButton);
        }

        [UnityTest, Description("Un click sul terreno durante l'avvicinamento annulla l'attacco")]
        public IEnumerator ClickOnGroundWhileApproaching_CancelsAttack()
        {
            yield return LoadArena();

            ClickAt(SkeletonAimPoint);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(_playerAttack.HasTarget);

            var ground = new Vector3(-4f, 0f, 0f);
            ClickAt(ground);
            yield return new WaitForSeconds(3f);

            Assert.IsFalse(_playerAttack.HasTarget, "il click sul terreno deve lasciare il bersaglio");
            Assert.AreEqual(_skeletonHealth.Max, _skeletonHealth.Current, "nessun colpo deve arrivare");
            Assert.Less(FlatDistance(Player.position, ground), 0.5f, $"il cavaliere deve andare sul punto cliccato, è in {Player.position}");
        }

        [UnityTest, Description("Se il bersaglio si allontana durante il fendente il danno non arriva")]
        public IEnumerator TargetLeavesDuringSwing_NoDamage()
        {
            yield return LoadArena();

            ClickAt(SkeletonAimPoint);
            float elapsed = 0f;
            while (elapsed < 6f && !_playerAttack.IsSwinging)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(_playerAttack.IsSwinging, "il colpo deve partire");

            _skeletonAgent.Warp(new Vector3(-3f, 0f, 0f));
            yield return new WaitForSeconds(_playerAttack.Weapon.HitDelay + 0.3f);

            Assert.AreEqual(_skeletonHealth.Max, _skeletonHealth.Current, "il colpo a vuoto non deve fare danno");
        }
    }
}
